using System;
using System.Collections.Generic;
using System.Text;

namespace ProtoMaster.Common.Models
{
    #region 枚举定义

    /// <summary>
    /// ACC功能状态枚举
    /// </summary>
    public enum AccState : uint
    {
        Off = 0x0,
        Passive = 0x1,
        Standby = 0x2,
        ActiveControl = 0x3,
        BrakeOnly = 0x4,
        Override = 0x5,
        StandWait = 0x6,
        Failure = 0x7,
        StandActive = 0x8
    }

    /// <summary>
    /// 巡航跟车加减速状态指示枚举
    /// </summary>
    public enum CruiseAccelerationState : uint
    {
        None = 0x0,
        Accelerating = 0x1,
        Decelerating = 0x2
    }

    /// <summary>
    /// 高速领航辅助状态 (HNOP) 枚举
    /// </summary>
    public enum HnopState : uint
    {
        Off = 0x0,
        Passive = 0x1,
        Ready = 0x2,
        ActiveControl = 0x3,
        LongitudinalSuspend = 0x4,
        TOR = 0x5, // Take Over Request
        SafeStop = 0x6,
        Failure = 0x7,
        LateralSuspend = 0x8
    }

    /// <summary>
    /// 领航辅助变道状态信息枚举
    /// </summary>
    public enum NopLaneChangeInfo : uint
    {
        None = 0x0,
        PrepareAlcLeft = 0x1,
        PrepareAlcRight = 0x2,
        OngoingAlcLeft = 0x3,
        OngoingAlcRight = 0x4,
        CancelAlc = 0x5,
        RequestDriverLcLeft = 0x6,
        RequestDriverLcRight = 0x7,
        LcFinished = 0x8,
        RequestDriverLc = 0x9,
        LcLeftNotAvailable = 0xA,
        LcRightNotAvailable = 0xB,
        LaneMergeLeft = 0xC,
        LaneMergeRight = 0xD,
        LaneSplitLeft = 0xE,
        LaneSplitRight = 0xF
    }

    /// <summary>
    /// ICA 工作状态枚举
    /// </summary>
    public enum IcaState : uint
    {
        Off = 0x0,
        Passive = 0x1,
        Standby = 0x2,
        Active = 0x3,
        Failure = 0x4,
        Suspend = 0x5,
        SafeStop = 0x6
    }

    /// <summary>
    /// 拨杆变道 (ALC) 功能状态枚举
    /// </summary>
    public enum AlcStatus : uint
    {
        Off = 0x0,
        Inhibited = 0x1,
        Enable = 0x2,
        Wait = 0x3,
        LaneChanging = 0x4,
        LaneChanged = 0x5,
        Abort = 0x6,
        Retreating = 0x7,
        Retreated = 0x8,
        LaneChangePrepare = 0x9,
        LaneChangeLongitudinalMatching = 0xA,
        Failure = 0xB
    }

    /// <summary>
    /// LKS 车道线跟踪状态枚举
    /// </summary>
    public enum LaneTrackingState : uint
    {
        Inactive = 0x0,
        LaneTracking = 0x1,
        Intervention = 0x2, // 干预
        Warning = 0x3       // 报警
    }

    /// <summary>
    /// ELK 激活状态枚举
    /// </summary>
    public enum ElkActiveState : uint
    {
        NoWarning = 0x0,
        Tracking = 0x1,
        Intervention = 0x2
    }

    /// <summary>
    /// APARPA 状态机信息枚举
    /// </summary>
    public enum ApaRpaState : uint
    {
        Off = 0x0,
        Standby = 0x1,
        Searching = 0x2,
        GuidanceActive = 0x3,
        Completed = 0x4,
        Failure = 0x5,
        Terminate = 0x6,
        Pause = 0x7,
        Undo = 0x8,
        Quit = 0x9,
        Reserved = 0x10 // 0x10-0xFE 预留
    }

    /// <summary>
    /// 泊入泊出方式选择反馈枚举
    /// </summary>
    public enum ParkingTypeSelect : uint
    {
        NoSelection = 0x0,
        PIC = 0x1,          // Parking In Control
        POC = 0x2,          // Parking Out Control
        FreePIC = 0x3       // Free Parking In Control
    }

    /// <summary>
    /// 循迹倒车 (MRA) 状态信息枚举
    /// </summary>
    public enum MraState : uint
    {
        Off = 0x0,
        Standby = 0x1,
        BackgroundRecord = 0x2,
        Preparing = 0x3,
        Guidance = 0x4,
        FinishAndReset = 0x5,
        Failure = 0x6,
        Terminate = 0x7,
        Pause = 0x8
    }

    /// <summary>
    /// 记忆泊车 (HPA) 状态信息枚举
    /// </summary>
    public enum HpaState : uint
    {
        Off = 0x0,
        Standby = 0x1,
        GuidanceActive = 0x2,
        Completed = 0x3,
        Failure = 0x4,
        Terminate = 0x5,
        Pause = 0x6,
        Training = 0x7,
        TrainingCompleted = 0x8,
        TrainingTerminate = 0x9,
        Mapping = 0xA,
        TrackPreparing = 0xB
    }

    /// <summary>
    /// 泊入方向状态枚举
    /// </summary>
    public enum ParkingDirection : uint
    {
        None = 0x0,
        HeadIn = 0x1,
        BackIn = 0x2
    }

    /// <summary>
    /// 车位可泊方向类型枚举
    /// </summary>
    public enum ParkingAvailableDirectionType : uint
    {
        None = 0x0,
        HeadIn = 0x1,
        BackIn = 0x2,
        HeadInAndBackIn = 0x3
    }

    #endregion

    /// <summary>
    /// 驾驶功能状态信息类
    /// </summary>
    public class CommonStateInfo
    {
        // 核心状态枚举
        public AccState accSts { get; set; }                        // ACC功能状态
        public CruiseAccelerationState cruiseAccelerateSts { get; set; } // 巡航跟车加减速状态
        public HnopState hnopSts { get; set; }                      // 高速领航辅助状态
        public NopLaneChangeInfo nopLaneChangeInfo { get; set; }    // 领航辅助变道状态信息
        public IcaState icaSts { get; set; }                        // ICA 工作状态
        public AlcStatus alcStatus { get; set; }                    // 拨杆变道功能状态

        // 变道目标位置信息
        public float alcDestPoseX { get; set; }                     // 变道目标位置X
        public float alcDestPoseY { get; set; }                     // 变道目标位置Y
        public float alcDestPoseHeading { get; set; }               // 变道目标位置航向角

        // LKS 跟踪状态
        public LaneTrackingState lksLeftTrackingSt { get; set; }    // 左侧跟踪状态
        public LaneTrackingState lksRightTrackingSt { get; set; }   // 右侧跟踪状态

        // ELK 激活状态
        public ElkActiveState elkLeftActiveSt { get; set; }         // ELK 左侧激活状态
        public ElkActiveState elkRightActiveSt { get; set; }        // ELK 右侧激活状态


        //Parking State
        public ApaRpaState apaRpaSts { get; set; }                  // APARPA状态机信息
        public ParkingTypeSelect parkingTypeSelectFb { get; set; }  // 泊入泊出方式选择反馈
        public float parkingStopDist { get; set; }                  // 泊车剩余距离 (单位: m)

        public MraState mraSts { get; set; }                        // 循迹倒车状态信息
        // 循迹倒车引导线
        public CommonTrajectoryPoints trajectoryPoints { get; set; } = new CommonTrajectoryPoints();

        public HpaState hpaSts { get; set; }                        // 记忆泊车状态信息
        public UInt32 hpaPathSelectIDInd { get; set; }              // 记忆泊车路线选择ID反馈

        public ParkingDirection parkinDirectionSts { get; set; }    // 泊入方向状态反馈
        public ParkingAvailableDirectionType parkInDirectionAvaType { get; set; } // 泊入车位的可泊方向类型
    }
}
