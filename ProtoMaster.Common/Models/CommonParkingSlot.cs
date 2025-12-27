using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace ProtoMaster.Common.Models
{
    /// <summary>
    /// 车位类型枚举
    /// </summary>
    public enum ParkingSlotType : uint
    {
        Undefined = 0,      // 未定义
        Parallel = 1,       // 水平
        Vertical = 2,       // 垂直
        Slanted = 3         // 斜列
    }

    /// <summary>
    /// 车位状态枚举
    /// </summary>
    public enum ParkingSlotStatus : uint
    {
        Unknown = 0x0,
        Empty = 0x1,
        Occupied = 0x2,
        Parkable = 0x3,
        Target = 0x4,
        NarrowTargetSlot = 0x5,
        OccupiedTargetSlot = 0x6,
        RegionalTargetSlot = 0x7,
        RegionalOccupiedTargetSlot = 0x8
    }

    /// <summary>
    /// 车位楼层枚举
    /// </summary>
    public enum ParkingSlotFloor : uint
    {
        None = 0x0,
        L1_Floor = 0x1,
        G1_Floor = 0x2,
        L2_Floor = 0x3,
        G2_Floor = 0x4,
        L3_Floor = 0x5,
        G3_Floor = 0x6,
        L4_Floor = 0x7,
        G4_Floor = 0x8,
        L5_Floor = 0x9,
        G5_Floor = 0xA
    }

    /// <summary>
    /// 车位感知信息类
    /// </summary>
    public class CommonParkingSlot
    {
        public UInt32 slotID { get; set; }                      // 车位ID
        public ParkingSlotType slotType { get; set; }           // 车位类型
        public ParkingSlotStatus slotStatus { get; set; }       // 车位状态

        // 车位四个顶点，使用通用路径/坐标点信息类
        public Vector3 slotPointTop1 { get; set; }         // 车位顶点1
        public Vector3 slotPointTop2 { get; set; }         // 车位顶点2
        public Vector3 slotPointBottom1 { get; set; }      // 车位顶点3
        public Vector3 slotPointBottom2 { get; set; }      // 车位顶点4

        public UInt32 slotNum { get; set; }                     // 车位序号
        public ParkingSlotFloor slotFloor { get; set; }         // 车位楼层
    }

    public class CommonParkingSlotList
    {
        public List<CommonParkingSlot> parkingSlotList { get; set; } = new List<CommonParkingSlot>(); // 车位集合
    }
}
