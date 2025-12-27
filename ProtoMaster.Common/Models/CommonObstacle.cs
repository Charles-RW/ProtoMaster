using System.Numerics;

namespace ProtoMaster.Common.Models
{
    // 通用枚举定义
    public enum CommonObjectType
    {
        // --- 动态交通参与者 (Dynamic Objects) ---
        Unknown = 0x00,
        Pedestrian = 0x01,              // 行人
        Cyclist = 0x02,                 // 骑自行车的人
        Car = 0x03,                     // 汽车
        Truck = 0x04,                   // 卡车
        Bus = 0x05,                     // 公共汽车
        Motorcycle = 0x06,              // 摩托车
        Bicycle = 0x07,                 // 自行车
        SUV = 0x08,                     // SUV
        LargeTruck = 0x09,              // 大型货车
        Tricycle = 0x0A,                // 三轮车
        SpecialOperationVehicle = 0x0B, // 特种作业车
        OtherVehicle = 0x0C,            // 其他车辆
        Motorcyclist = 0x0D,            // 骑摩托车的人
        ElectricBicycle = 0x0E,         // 载人电动自行车
        NoRiderElectricBicycle = 0x0F,  // 非载人电动自行车
        NoRiderMotorcycle = 0x10,       // 非载人摩托车
        NoRiderTricycle = 0x11,         // 非载人三轮车
        IrregularCar = 0x12,            // 异型车
        LargeAnimals = 0x13,            // 静止大中型动物
        SmallAnimals = 0x14,            // 静止小型动物
        DynamicLargeAnimals = 0x15,     // 动态大中型动物
        DynamicSmallAnimals = 0x16,     // 动态小型动物
        DynamicPedestrian = 0x17,       // 动态行人
        NoRiderBicycle = 0x18,          // 非载人自行车
        SmallTruck = 0x19,              // 小货车
        Van = 0x1A,                     // 面包车
        EngineeringVehicle = 0x1B,      // 工程车
        Deliveryman = 0x1C,             // 外卖员
        Policeman = 0x1D,               // 警察
        PoliceCar = 0x1E,               // 警车
        FireFightingTruck = 0x1F,       // 消防车

        // --- 静态障碍物/设施 (Static Obstacles) ---
        ConeBucket = 0x101,              // 锥桶
        WaterHorse = 0x102,              // 水马
        TriangularWarningSign = 0x103,   // 三角牌
        ParkingBarrierClosed = 0x104,    // 停车杆合状态
        SpeedBump = 0x105,               // 减速带
        ParkingLock = 0x106,             // 地锁
        SquareColumn = 0x107,            // 方柱
        CircularColumn = 0x108,          // 圆柱
        Pole = 0x109,                    // 杆
        ParkingBarrierOpen = 0x10A,      // 停车杆开状态
        TrafficPost = 0x10B,             // 交通柱
        IsolationPost = 0x10C,           // 隔离柱
        CrashCushionBarrel = 0x10D,      // 防撞桶
        ConstructionDiversionSign = 0x10E, // 施工导流标志牌
        ParkingSign = 0x10F,             // 停车标志牌
        ChargingPile = 0x110,            // 充电桩
        StonePier = 0x111,               // 石墩
        Limiter = 0x112,                 // 限位器
        HeightRestrictionBarrier = 0x113,// 限高杆
        TrafficlightPassing = 0x114,     // 车道信号灯可通行
        TrafficlightNoPassing = 0x115,   // 车道信号灯不可通行
        OtherStaticObject = 0x116        // 其他
    }

    public enum CommonCarLightStatus
    {
        Na = 0x0,               //（无）
        Turnleft = 0x1,         //（左转）
        Turnright = 0x2,        //（右转）
        DoubleFlash = 0x3,      //（双闪）
        Brake = 0x4,            //（刹车）
        Reverse = 0x5,          //(倒车灯）
        Turnleft_Brake = 0x6,   //（左转+刹车）
        Turnright_Brake = 0x7   //（右转+刹车）
    }

    // 通用数据类
    public class CommonObstacle
    {
        public int Id { get; set; }
        public CommonObjectType Type { get; set; }
        
        // 统一使用 Vector3 或自定义结构描述坐标，解决层级不一致问题
        public Vector3 Position { get; set; } 
        public Vector3 Velocity { get; set; }
        public double Width {  get; set; }
        public double Height { get; set; }
        public double Length { get; set; }
        public double heading { get; set; }
        public CommonCarLightStatus CarLightStatus { get; set; }
        public CommonColor Color { get; set; }
        public UInt64 Timestamp { get; set; }
        public UInt64 LaneId { get; set; }

        // 预留扩展字段（字典），用于存储无法归一化的特有数据
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    public class CommonObstacleList
    {
        public List<CommonObstacle> Obstacles { get; set; } = new List<CommonObstacle>();
    }
}