using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace SmartStayBLL
{
    public sealed class CloudinaryImageStorageService
        : IImageStorageService
    {
        private const long MaximumFileSizeInBytes =
            5 * 1024 * 1024;

        private const int MaximumImageDimension = 2000;

        private static readonly HashSet<string>
            AllowedExtensions =
            new(StringComparer.OrdinalIgnoreCase)
            {
                ".jpg",
                ".jpeg",
                ".png",
                ".webp"
            };

        private static readonly byte[] JpegSignature =
        {
            0xFF,
            0xD8,
            0xFF
        };

        private static readonly byte[] PngSignature =
        {
            0x89,
            0x50,
            0x4E,
            0x47,
            0x0D,
            0x0A,
            0x1A,
            0x0A
        };

        private static readonly byte[] RiffSignature =
        {
            0x52,
            0x49,
            0x46,
            0x46
        };

        private static readonly byte[] WebpSignature =
        {
            0x57,
            0x45,
            0x42,
            0x50
        };

        private readonly Cloudinary _cloudinary;
        private readonly HttpClient _httpClient;
        private readonly ILogger<CloudinaryImageStorageService>
            _logger;

        public CloudinaryImageStorageService(
     HttpClient httpClient,
     IOptions<CloudinarySettings> options,
     ILogger<CloudinaryImageStorageService> logger)
        {
            ArgumentNullException.ThrowIfNull(
                httpClient);

            ArgumentNullException.ThrowIfNull(
                options);

            ArgumentNullException.ThrowIfNull(
                logger);

            var settings =
                options.Value;

            var account =
                new Account(
                    settings.CloudName,
                    settings.ApiKey,
                    settings.ApiSecret);

            _cloudinary =
                new Cloudinary(account);

            _cloudinary.Api.Secure =
                true;

            _httpClient =
                httpClient;

            _logger =
                logger;
        }
        public async Task<ImageUploadResult> UploadAsync(
     Stream fileStream,
     string fileName,
     string contentType,
     string folder,
     ImageAccessType accessType,
     CancellationToken cancellationToken = default)
        {
            ValidateBasicInputs(
                fileStream,
                fileName,
                contentType,
                folder);

            var extension =
                Path.GetExtension(fileName)
                    .ToLowerInvariant();

            ValidateExtension(extension);

            ValidateContentType(
                extension,
                contentType);

            await using var validatedStream =
                await CopyAndValidateFileAsync(
                    fileStream,
                    extension,
                    cancellationToken);

            var normalizedFolder =
                NormalizeFolder(folder);

            var imageIdentifier =
                Guid.NewGuid().ToString("N");

            var publicId =
                $"{normalizedFolder}/{imageIdentifier}";

            var cloudinaryDeliveryType =
                GetCloudinaryDeliveryType(accessType);

            var uploadParameters =
                new ImageUploadParams
                {
                    File = new FileDescription(
                        fileName,
                        validatedStream),

                    PublicId = publicId,

                    AssetFolder = normalizedFolder,

                    DisplayName = imageIdentifier,

                    UseFilename = false,

                    UniqueFilename = false,

                    Overwrite = false,

                    Type = cloudinaryDeliveryType,

                    AllowedFormats =
                    [
                        "jpg",
                "jpeg",
                "png",
                "webp"
                    ],

                    Transformation =
                        new Transformation()
                            .Width(MaximumImageDimension)
                            .Height(MaximumImageDimension)
                            .Crop("limit")
                };

            try
            {
                var uploadResult =
                    await _cloudinary.UploadAsync(
                        uploadParameters,
                        cancellationToken);

                if (uploadResult.Error is not null)
                {
                    _logger.LogError(
                        "Cloudinary failed to upload image {FileName}. " +
                        "Provider error: {ProviderError}",
                        fileName,
                        uploadResult.Error.Message);

                    throw new InvalidOperationException(
                        "The image could not be uploaded.");
                }

                if (uploadResult.SecureUrl is null ||
                    string.IsNullOrWhiteSpace(
                        uploadResult.PublicId))
                {
                    _logger.LogError(
                        "Cloudinary returned an incomplete response " +
                        "while uploading image {FileName}.",
                        fileName);

                    throw new InvalidOperationException(
                        "The image provider returned an invalid response.");
                }

                return new ImageUploadResult
                {
                    SecureUrl =
                        uploadResult.SecureUrl.ToString(),

                    PublicId =
                        uploadResult.PublicId,

                    Format =
                        uploadResult.Format ??
                        extension.TrimStart('.'),

                    FileSizeInBytes =
                        uploadResult.Bytes,

                    Width =
                        uploadResult.Width,

                    Height =
                        uploadResult.Height
                };
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (InvalidOperationException)
            {
                throw;
            }
            catch (Exception exception)
            {
                _logger.LogError(
                    exception,
                    "An unexpected error occurred while uploading " +
                    "image {FileName} to Cloudinary.",
                    fileName);

                throw new InvalidOperationException(
                    "An unexpected error occurred while uploading the image.",
                    exception);
            }
        }

        public async Task<ImageDeletionResult> DeleteAsync(
    string publicId,
    ImageAccessType accessType,
    CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(publicId))
            {
                throw new ArgumentException(
                    "The image public ID is required.",
                    nameof(publicId));
            }

            var normalizedPublicId =
                NormalizePublicId(publicId);

            var cloudinaryDeliveryType =
                GetCloudinaryDeliveryType(accessType);

            cancellationToken.ThrowIfCancellationRequested();

            var deletionParameters =
                new DeletionParams(normalizedPublicId)
                {
                    ResourceType = ResourceType.Image,

                    Type = cloudinaryDeliveryType,

                    Invalidate = true
                };

            try
            {
                var deletionResult =
                    await _cloudinary.DestroyAsync(
                        deletionParameters);

                if (deletionResult.Error is not null)
                {
                    _logger.LogError(
                        "Cloudinary failed to delete image {PublicId}. " +
                        "Provider error: {ProviderError}",
                        normalizedPublicId,
                        deletionResult.Error.Message);

                    throw new InvalidOperationException(
                        "The image could not be deleted.");
                }

                var providerResult =
                    deletionResult.Result ?? string.Empty;

                var deletionSucceeded =
                    string.Equals(
                        providerResult,
                        "ok",
                        StringComparison.OrdinalIgnoreCase)
                    ||
                    string.Equals(
                        providerResult,
                        "not found",
                        StringComparison.OrdinalIgnoreCase);

                if (!deletionSucceeded)
                {
                    _logger.LogError(
                        "Cloudinary returned an unexpected result " +
                        "while deleting image {PublicId}. " +
                        "Provider result: {ProviderResult}",
                        normalizedPublicId,
                        providerResult);

                    throw new InvalidOperationException(
                        "The image provider returned an unexpected deletion result.");
                }

                _logger.LogInformation(
                    "Cloudinary image {PublicId} was deleted. " +
                    "Provider result: {ProviderResult}",
                    normalizedPublicId,
                    providerResult);

                return new ImageDeletionResult
                {
                    IsDeleted = true,
                    PublicId = normalizedPublicId,
                    ProviderResult = providerResult
                };
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (InvalidOperationException)
            {
                throw;
            }
            catch (Exception exception)
            {
                _logger.LogError(
                    exception,
                    "An unexpected error occurred while deleting " +
                    "Cloudinary image {PublicId}.",
                    normalizedPublicId);

                throw new InvalidOperationException(
                    "An unexpected error occurred while deleting the image.",
                    exception);
            }
        }






        public async Task<ImageContentResult> DownloadAsync(
    string publicId,
    string format,
    ImageAccessType accessType,
    CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(publicId))
            {
                throw new ArgumentException(
                    "The image public ID is required.",
                    nameof(publicId));
            }

            if (string.IsNullOrWhiteSpace(format))
            {
                throw new ArgumentException(
                    "The image format is required.",
                    nameof(format));
            }

            var normalizedPublicId =
                NormalizePublicId(publicId);

            var normalizedFormat =
                NormalizeFormat(format);

            var cloudinaryDeliveryType =
                GetCloudinaryDeliveryType(
                    accessType);

            /*
             * The URL is generated and used only inside the backend.
             * It is never returned to the frontend.
             */
            var signedUrl =
                _cloudinary.Api.UrlImgUp
                    .Secure()
                    .Type(cloudinaryDeliveryType)
                    .Signed(true)
                    .Format(normalizedFormat)
                    .BuildUrl(normalizedPublicId);

            try
            {
                using var response =
                    await _httpClient.GetAsync(
                        signedUrl,
                        HttpCompletionOption.ResponseHeadersRead,
                        cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError(
                        "Cloudinary failed to return image {PublicId}. " +
                        "Provider status code: {StatusCode}",
                        normalizedPublicId,
                        (int)response.StatusCode);

                    throw new InvalidOperationException(
                        "The image could not be retrieved.");
                }

                var content =
                    await response.Content
                        .ReadAsByteArrayAsync(
                            cancellationToken);

                if (content.Length == 0)
                {
                    _logger.LogError(
                        "Cloudinary returned empty content for image {PublicId}.",
                        normalizedPublicId);

                    throw new InvalidOperationException(
                        "The image provider returned empty content.");
                }

                var providerContentType =
                    response.Content.Headers
                        .ContentType?
                        .MediaType;

                return new ImageContentResult
                {
                    Content =
                        content,

                    ContentType =
                        string.IsNullOrWhiteSpace(
                            providerContentType)
                            ? GetImageContentType(
                                normalizedFormat)
                            : providerContentType
                };
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (InvalidOperationException)
            {
                throw;
            }
            catch (Exception exception)
            {
                _logger.LogError(
                    exception,
                    "An unexpected error occurred while retrieving " +
                    "Cloudinary image {PublicId}.",
                    normalizedPublicId);

                throw new InvalidOperationException(
                    "An unexpected error occurred while retrieving the image.",
                    exception);
            }
        }






        private static string GetImageContentType(
    string format)
        {
            return format switch
            {
                "jpg" or "jpeg" =>
                    "image/jpeg",

                "png" =>
                    "image/png",

                "webp" =>
                    "image/webp",

                _ =>
                    "application/octet-stream"
            };
        }


        private static string NormalizeFormat(
    string format)
        {
            var normalizedFormat =
                format.Trim()
                    .TrimStart('.')
                    .ToLowerInvariant();

            var isSupported =
                normalizedFormat is
                    "jpg"
                    or "jpeg"
                    or "png"
                    or "webp";

            if (!isSupported)
            {
                throw new ArgumentException(
                    "The image format is not supported.",
                    nameof(format));
            }

            return normalizedFormat;
        }










        private static void ValidateBasicInputs(
            Stream fileStream,
            string fileName,
            string contentType,
            string folder)
        {
            ArgumentNullException.ThrowIfNull(fileStream);

            if (!fileStream.CanRead)
            {
                throw new ArgumentException(
                    "The image stream cannot be read.",
                    nameof(fileStream));
            }

            if (string.IsNullOrWhiteSpace(fileName))
            {
                throw new ArgumentException(
                    "The image file name is required.",
                    nameof(fileName));
            }

            if (string.IsNullOrWhiteSpace(contentType))
            {
                throw new ArgumentException(
                    "The image content type is required.",
                    nameof(contentType));
            }

            if (string.IsNullOrWhiteSpace(folder))
            {
                throw new ArgumentException(
                    "The target image folder is required.",
                    nameof(folder));
            }
        }

        private static void ValidateExtension(
            string extension)
        {
            if (!AllowedExtensions.Contains(extension))
            {
                throw new ArgumentException(
                    "Only JPG, JPEG, PNG, and WebP images are allowed.");
            }
        }

        private static void ValidateContentType(
            string extension,
            string contentType)
        {
            var expectedContentType =
                extension switch
                {
                    ".jpg" or ".jpeg" =>
                        "image/jpeg",

                    ".png" =>
                        "image/png",

                    ".webp" =>
                        "image/webp",

                    _ =>
                        string.Empty
                };

            if (!string.Equals(
                    contentType,
                    expectedContentType,
                    StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException(
                    "The image extension does not match its content type.");
            }
        }

        private static async Task<MemoryStream>
            CopyAndValidateFileAsync(
                Stream sourceStream,
                string extension,
                CancellationToken cancellationToken)
        {
            var memoryStream = new MemoryStream();

            var buffer = new byte[81920];

            long totalBytesRead = 0;

            try
            {
                while (true)
                {
                    var bytesRead =
                        await sourceStream.ReadAsync(
                            buffer.AsMemory(
                                0,
                                buffer.Length),
                            cancellationToken);

                    if (bytesRead == 0)
                    {
                        break;
                    }

                    totalBytesRead += bytesRead;

                    if (totalBytesRead >
                        MaximumFileSizeInBytes)
                    {
                        throw new ArgumentException(
                            "The image size must not exceed 5 MB.");
                    }

                    await memoryStream.WriteAsync(
                        buffer.AsMemory(
                            0,
                            bytesRead),
                        cancellationToken);
                }

                if (totalBytesRead == 0)
                {
                    throw new ArgumentException(
                        "The uploaded image is empty.");
                }

                memoryStream.Position = 0;

                ValidateFileSignature(
                    memoryStream,
                    extension);

                memoryStream.Position = 0;

                return memoryStream;
            }
            catch
            {
                await memoryStream.DisposeAsync();

                throw;
            }
        }

        private static void ValidateFileSignature(
            Stream stream,
            string extension)
        {
            Span<byte> header = stackalloc byte[12];

            var bytesRead = stream.Read(header);

            stream.Position = 0;

            var isValid =
                extension switch
                {
                    ".jpg" or ".jpeg" =>
                        bytesRead >=
                        JpegSignature.Length &&
                        header[..JpegSignature.Length]
                            .SequenceEqual(
                                JpegSignature),

                    ".png" =>
                        bytesRead >=
                        PngSignature.Length &&
                        header[..PngSignature.Length]
                            .SequenceEqual(
                                PngSignature),

                    ".webp" =>
                        bytesRead >= 12 &&
                        header[..RiffSignature.Length]
                            .SequenceEqual(
                                RiffSignature) &&
                        header[8..12]
                            .SequenceEqual(
                                WebpSignature),

                    _ =>
                        false
                };

            if (!isValid)
            {
                throw new ArgumentException(
                    "The uploaded file is not a valid image.");
            }
        }

        private static string NormalizeFolder(
            string folder)
        {
            var normalizedFolder =
                folder.Trim()
                    .Trim('/');

            if (string.IsNullOrWhiteSpace(
                    normalizedFolder))
            {
                throw new ArgumentException(
                    "The target image folder is invalid.",
                    nameof(folder));
            }

            if (normalizedFolder.Contains(
                    "..",
                    StringComparison.Ordinal))
            {
                throw new ArgumentException(
                    "The target image folder is invalid.",
                    nameof(folder));
            }

            return normalizedFolder;
        }
        private static string NormalizePublicId(
                           string publicId)
        {
            var normalizedPublicId =
                publicId.Trim()
                    .Trim('/');

            if (string.IsNullOrWhiteSpace(
                    normalizedPublicId))
            {
                throw new ArgumentException(
                    "The image public ID is invalid.",
                    nameof(publicId));
            }

            if (normalizedPublicId.Contains(
                    "..",
                    StringComparison.Ordinal))
            {
                throw new ArgumentException(
                    "The image public ID is invalid.",
                    nameof(publicId));
            }

            return normalizedPublicId;
        }
        private static string GetCloudinaryDeliveryType(
    ImageAccessType accessType)
        {
            return accessType switch
            {
                ImageAccessType.Public =>
                    "upload",

                ImageAccessType.Authenticated =>
                    "authenticated",

                _ =>
                    throw new ArgumentOutOfRangeException(
                        nameof(accessType),
                        accessType,
                        "The image access type is not supported.")
            };
        }
    }
}