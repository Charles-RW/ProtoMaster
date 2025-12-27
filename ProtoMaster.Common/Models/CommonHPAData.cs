using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace ProtoMaster.Common.Models
{
    public class CommonHPAData
    {
        public HPAPathDetail HPAPathDetail { get; set; } = new HPAPathDetail();
        public HPARepalyPathInfo HPARepalyPathInfo { get; set; } = new HPARepalyPathInfo();
        public HPATrainingPathInfo HPATrainingPathInfo { get; set; } = new HPATrainingPathInfo();
    }

    #region 枚举定义

    /// <summary>
    /// 路线学习状态枚举
    /// </summary>
    public enum HpaPathState : uint
    {
        NotActive = 0x0,
        Saving = 0x1,
        Saved = 0x2,
        Failed = 0x3,
        Invalid = 0xFF
    }

    /// <summary>
    /// 路线匹配状态枚举
    /// </summary>
    public enum HpaPathMatchStatus : uint
    {
        Fail = 0x0,
        Success = 0x1
    }

    /// <summary>
    /// 路线标签枚举
    /// </summary>
    public enum HpaPathLabel : uint
    {
        NoLabel = 0x0,
        Home = 0x1,
        Office = 0x2,
        Other = 0x3
    }

    /// <summary>
    /// 路线模式枚举
    /// </summary>
    public enum HpaPathType : uint
    {
        None = 0x0,
        HpaParkIn = 0x1,
        HpaParkOut = 0x2
    }

    /// <summary>
    /// 记忆泊车路径标签类型枚举
    /// </summary>
    public enum HpaOnWayLabelType : uint
    {
        Elevator = 0,   // 电梯
        Entrance = 1,   // 入口
        Exit = 2,       // 出口
        Charging = 3    // 充电
    }

    #endregion


    /// <summary>
    /// 记忆泊车路径标签信息类
    /// </summary>
    public class HpaOnWayLabel
    {
        public UInt32 hpaOnWayLabelID { get; set; }             // 记忆泊车路径标签ID
        public HpaOnWayLabelType hpaOnWayLabelType { get; set; } // 记忆泊车路径标签类型

        // 记忆泊车路径标签坐标 (对应 Vector3)
        public Vector3 hpaOnWayLabelPoint { get; set; }

        public UInt32 hpaOnWayLabelPointNum { get; set; }       // 记忆泊车路径标签数量
    }

    public class HPAPathDetail
    {
        public UInt32 hpaPathID { get; set; }                       // 路线ID
        public HpaPathState pathState { get; set; }                 // 路线学习状态
        public string pathName { get; set; }                        // 路线名称
        public UInt32 pathSaveProgress { get; set; }                // 路线保存进度
        public float pathLength { get; set; }                       // 路线长度

        // 路线学习点集
        public List<CommonTrajectoryPoints> linePoint { get; set; } = new List<CommonTrajectoryPoints>();

        public HpaPathMatchStatus pathMatchSts { get; set; }        // 路线匹配状态
        public HpaPathLabel pathLabel { get; set; }                 // 路线标签
        public HpaPathType hpaPathType { get; set; }                // 路线模式

        // HPA 沿途标签显示 (对应 ADAS_arr_HPAOnWayLabelReplay)
        public List<HpaOnWayLabel> hpaOnWayLabelReplay { get; set; } = new List<HpaOnWayLabel>();

        // 显示路线中的车位 (对应 ADAS_arr_HPAPathslot)
        public CommonParkingSlotList CommonParkingSlotList { get; set; } = new CommonParkingSlotList();

        // 显示路线中的静态障碍物 (对应 ADAS_arr_HPAParkingStaticObjects)
        public CommonObstacleList CommonObstacleList { get; set; } = new CommonObstacleList();
    }

    public class HPATrainingPathInfo
    {
        public UInt32 hpaTrainingPathID { get; set; }               // 路线ID
        public string trainingPathName { get; set; }                // 路线名称
        public float hpaTrainingPathCurrentDist { get; set; }       // 路线学习实时累计距离 (单位: m)
        public float trainingPathLength { get; set; }               // 路线总长度 (单位: m)

        // 路线学习点集 (对应 ADAS_arr_PathPoint)
        public CommonTrajectoryPoints commonTrajectoryPoints { get; set; } = new CommonTrajectoryPoints();

        // 路线记忆车位 (对应 ADAS_arr_HPAPathslot)
        public CommonParkingSlotList CommonParkingSlotList { get; set; } = new CommonParkingSlotList();

        public HpaPathLabel trainingPathLabel { get; set; }         // 路线标签 (复用之前定义的 HpaPathLabel)

        // 静态障碍物显示 (对应 ADAS_arr_HPAParkingStaticObjects)
        public CommonObstacleList CommonObstacleList { get; set; } = new CommonObstacleList();
    }

    public class HPARepalyPathInfo
    {
        public UInt32 hpaReplayPathID { get; set; }                 // 路线ID
        public string replayPathName { get; set; }                  // 路线名称
        public float replayDrivingDist { get; set; }                // 路线已行驶距离 (单位: m)
        public float hpaRepayRemainingDist { get; set; }            // 记忆泊车回放剩余距离 (单位: m)

        // 路线点集 (复用 AdasPathPoint)
        public CommonTrajectoryPoints commonTrajectoryPoints { get; set; } = new CommonTrajectoryPoints();

        // 路线记忆车位 (复用 AdasHpaPathSlot)
        public CommonParkingSlotList CommonParkingSlotList { get; set; } = new CommonParkingSlotList();

        // 路线标签 (复用 HpaPathLabel: Home/Office/Other)
        public HpaPathLabel replayPathLabel { get; set; }

        // HPA 终点信息
        public Vector3 hpaEndPos { get; set; }

        // 静态障碍物回放 (复用 AdasHpaStaticObject)
        public CommonObstacleList CommonObstacleList { get; set; } = new CommonObstacleList();

        // 路面标记回放 (复用之前定义的 AdasRoadMarker)
        public CommonRoadMarkerList CommonRoadMarkerList { get; set; } = new CommonRoadMarkerList();

        // 车道线回放 (复用之前定义的 AdasLaneLine)
        public CommonLaneLineList CommonLaneLineList { get; set; } = new CommonLaneLineList();
    }
}
