namespace SmartStayDAL
{
    public static class AmenitySeedData
    {
        public static readonly Amenity[] All =
        [
            /*
             * Essentials
             */

            Create(
                "10000000-0000-0000-0000-000000000001",
                "wifi",
                "Wi-Fi",
                AmenityCategory.Essentials,
                "wifi",
                1),

            Create(
                "10000000-0000-0000-0000-000000000002",
                "air_conditioning",
                "Air Conditioning",
                AmenityCategory.Essentials,
                "snowflake",
                2),

            Create(
                "10000000-0000-0000-0000-000000000003",
                "heating",
                "Heating",
                AmenityCategory.Essentials,
                "flame",
                3),

            Create(
                "10000000-0000-0000-0000-000000000004",
                "washer",
                "Washer",
                AmenityCategory.Essentials,
                "washer",
                4),

            Create(
                "10000000-0000-0000-0000-000000000005",
                "iron",
                "Iron",
                AmenityCategory.Essentials,
                "iron",
                5),

            Create(
                "10000000-0000-0000-0000-000000000006",
                "workspace",
                "Dedicated Workspace",
                AmenityCategory.Essentials,
                "briefcase",
                6),

            Create(
                "10000000-0000-0000-0000-000000000007",
                "bed_linens",
                "Bed Linens",
                AmenityCategory.Essentials,
                "bed",
                7),

            /*
             * Kitchen and Dining
             */

            Create(
                "20000000-0000-0000-0000-000000000001",
                "kitchen",
                "Kitchen",
                AmenityCategory.KitchenAndDining,
                "cooking-pot",
                1),

            Create(
                "20000000-0000-0000-0000-000000000002",
                "refrigerator",
                "Refrigerator",
                AmenityCategory.KitchenAndDining,
                "refrigerator",
                2),

            Create(
                "20000000-0000-0000-0000-000000000003",
                "microwave",
                "Microwave",
                AmenityCategory.KitchenAndDining,
                "microwave",
                3),

            Create(
                "20000000-0000-0000-0000-000000000004",
                "oven",
                "Oven",
                AmenityCategory.KitchenAndDining,
                "oven",
                4),

            Create(
                "20000000-0000-0000-0000-000000000005",
                "stove",
                "Stove",
                AmenityCategory.KitchenAndDining,
                "stove",
                5),

            Create(
                "20000000-0000-0000-0000-000000000006",
                "coffee_maker",
                "Coffee Maker",
                AmenityCategory.KitchenAndDining,
                "coffee",
                6),

            Create(
                "20000000-0000-0000-0000-000000000007",
                "kettle",
                "Electric Kettle",
                AmenityCategory.KitchenAndDining,
                "kettle",
                7),

            Create(
                "20000000-0000-0000-0000-000000000008",
                "dining_area",
                "Dining Area",
                AmenityCategory.KitchenAndDining,
                "utensils",
                8),

            /*
             * Bathroom
             */

            Create(
                "30000000-0000-0000-0000-000000000001",
                "hot_water",
                "Hot Water",
                AmenityCategory.Bathroom,
                "shower-head",
                1),

            Create(
                "30000000-0000-0000-0000-000000000002",
                "hair_dryer",
                "Hair Dryer",
                AmenityCategory.Bathroom,
                "wind",
                2),

            Create(
                "30000000-0000-0000-0000-000000000003",
                "bathtub",
                "Bathtub",
                AmenityCategory.Bathroom,
                "bath",
                3),

            Create(
                "30000000-0000-0000-0000-000000000004",
                "towels",
                "Towels",
                AmenityCategory.Bathroom,
                "towel",
                4),

            Create(
                "30000000-0000-0000-0000-000000000005",
                "toiletries",
                "Toiletries",
                AmenityCategory.Bathroom,
                "package",
                5),

            /*
             * Entertainment
             */

            Create(
                "40000000-0000-0000-0000-000000000001",
                "tv",
                "TV",
                AmenityCategory.Entertainment,
                "tv",
                1),

            Create(
                "40000000-0000-0000-0000-000000000002",
                "streaming_services",
                "Streaming Services",
                AmenityCategory.Entertainment,
                "play",
                2),

            Create(
                "40000000-0000-0000-0000-000000000003",
                "books",
                "Books",
                AmenityCategory.Entertainment,
                "book-open",
                3),

            Create(
                "40000000-0000-0000-0000-000000000004",
                "board_games",
                "Board Games",
                AmenityCategory.Entertainment,
                "gamepad",
                4),

            /*
             * Outdoor
             */

            Create(
                "50000000-0000-0000-0000-000000000001",
                "balcony",
                "Balcony",
                AmenityCategory.Outdoor,
                "building",
                1),

            Create(
                "50000000-0000-0000-0000-000000000002",
                "garden",
                "Garden",
                AmenityCategory.Outdoor,
                "trees",
                2),

            Create(
                "50000000-0000-0000-0000-000000000003",
                "patio",
                "Patio",
                AmenityCategory.Outdoor,
                "armchair",
                3),

            Create(
                "50000000-0000-0000-0000-000000000004",
                "bbq_area",
                "BBQ Area",
                AmenityCategory.Outdoor,
                "cooking-pot",
                4),

            Create(
                "50000000-0000-0000-0000-000000000005",
                "swimming_pool",
                "Swimming Pool",
                AmenityCategory.Outdoor,
                "waves",
                5),

            /*
             * Parking and Access
             */

            Create(
                "60000000-0000-0000-0000-000000000001",
                "free_parking",
                "Free Parking",
                AmenityCategory.ParkingAndAccess,
                "car",
                1),

            Create(
                "60000000-0000-0000-0000-000000000002",
                "paid_parking",
                "Paid Parking",
                AmenityCategory.ParkingAndAccess,
                "circle-dollar-sign",
                2),

            Create(
                "60000000-0000-0000-0000-000000000003",
                "street_parking",
                "Street Parking",
                AmenityCategory.ParkingAndAccess,
                "parking-circle",
                3),

            Create(
                "60000000-0000-0000-0000-000000000004",
                "private_entrance",
                "Private Entrance",
                AmenityCategory.ParkingAndAccess,
                "door-open",
                4),

            /*
             * Safety
             */

            Create(
                "70000000-0000-0000-0000-000000000001",
                "smoke_alarm",
                "Smoke Alarm",
                AmenityCategory.Safety,
                "alarm-smoke",
                1),

            Create(
                "70000000-0000-0000-0000-000000000002",
                "carbon_monoxide_alarm",
                "Carbon Monoxide Alarm",
                AmenityCategory.Safety,
                "badge-alert",
                2),

            Create(
                "70000000-0000-0000-0000-000000000003",
                "fire_extinguisher",
                "Fire Extinguisher",
                AmenityCategory.Safety,
                "fire-extinguisher",
                3),

            Create(
                "70000000-0000-0000-0000-000000000004",
                "first_aid_kit",
                "First Aid Kit",
                AmenityCategory.Safety,
                "briefcase-medical",
                4),

            Create(
                "70000000-0000-0000-0000-000000000005",
                "safe",
                "Safe",
                AmenityCategory.Safety,
                "lock-keyhole",
                5),

            /*
             * Accessibility
             */

            Create(
                "80000000-0000-0000-0000-000000000001",
                "elevator",
                "Elevator",
                AmenityCategory.Accessibility,
                "arrow-up-down",
                1),

            Create(
                "80000000-0000-0000-0000-000000000002",
                "step_free_entrance",
                "Step-Free Entrance",
                AmenityCategory.Accessibility,
                "move-horizontal",
                2),

            Create(
                "80000000-0000-0000-0000-000000000003",
                "wheelchair_accessible",
                "Wheelchair Accessible",
                AmenityCategory.Accessibility,
                "accessibility",
                3),

            Create(
                "80000000-0000-0000-0000-000000000004",
                "accessible_parking",
                "Accessible Parking",
                AmenityCategory.Accessibility,
                "square-parking",
                4),

            Create(
                "80000000-0000-0000-0000-000000000005",
                "wide_doorways",
                "Wide Doorways",
                AmenityCategory.Accessibility,
                "door-open",
                5)
        ];

        private static Amenity Create(
            string id,
            string code,
            string name,
            AmenityCategory category,
            string iconKey,
            int displayOrder)
        {
            return new Amenity
            {
                Id = Guid.Parse(id),
                Code = code,
                Name = name,
                Category = category,
                IconKey = iconKey,
                DisplayOrder = displayOrder,
                IsActive = true
            };
        }
    }
}