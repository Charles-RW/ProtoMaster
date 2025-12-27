using System;
using System.Collections.Generic;
using System.Text;

namespace ProtoMaster.Common.Models
{
    /// <summary>
    /// 路面标记类型枚举
    /// </summary>
    public enum RoadMarkerType : uint
    {
        Unknown = 0x0,
        ArrowUp = 0x1,
        ArrowLeft = 0x2,
        ArrowRight = 0x3,
        ArrowUpLeft = 0x4,
        ArrowUpRight = 0x5,
        ArrowLeftRight = 0x6,
        ArrowLeftUpRight = 0x7,
        ArrowUTurn = 0x8,
        ArrowUpUTurn = 0x9,
        ArrowLeftUTurn = 0xA,
        ArrowLeftMerge = 0xB,
        ArrowRightMerge = 0xC,
        ArrowProhibition = 0xD,
        ArrowDashedLine = 0xE,
        Crosswalk = 0xF,
        Stopline = 0x10,
        VirtualStopline = 0x11,
        ArrowProhibitionLeft = 0x12,
        ArrowProhibitionRight = 0x13,
        ArrowProhibitionUTurn = 0x14,
        ArrowBicycleLane = 0x15,
        ArrowBusLane = 0x16,
        ArrowDecelerationZone = 0x17,
        YellowGridLineZone = 0x18,          // 黄色网格线
        Reserved = 0x19                     // 0x19-0xFE 预留
    }


    /// <summary>
    /// 路面标记信息类
    /// </summary>
    public class CommonRoadMarker
    {
        public RoadMarkerType roadMarkerType { get; set; }          // 路面标记类型
        public UInt64 roadMarkerID { get; set; }                    // 路面标记ID
        public UInt32 roadMarkerTrackingSts { get; set; }           // 路面标记跟踪状态

        public float roadMarkerPose1X { get; set; }                 // 路面标记点1X坐标
        public float roadMarkerPose1Y { get; set; }                 // 路面标记点1Y坐标
        public float roadMarkerPose2X { get; set; }                 // 路面标记点2X坐标
        public float roadMarkerPose2Y { get; set; }                 // 路面标记点2Y坐标

        public UInt32 roadMarkerWidth { get; set; }                 // 路面标记(人行道）宽度

        public float roadMarkerPose3X { get; set; }                 // 路面标记点3X坐标
        public float roadMarkerPose3Y { get; set; }                 // 路面标记点3Y坐标
        public float roadMarkerPose4X { get; set; }                 // 路面标记点4X坐标
        public float roadMarkerPose4Y { get; set; }                 // 路面标记点4Y坐标

        public CommonColor roadMarkerColor { get; set; }        // 路面标记颜色
    }

    public class CommonRoadMarkerList
    {
        public List<CommonRoadMarker> roadMarkerList { get; set; } = new List<CommonRoadMarker>(); // 路面标记集合
    }
}
