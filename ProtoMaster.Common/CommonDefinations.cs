using System.Collections.Generic;
using ProtoMaster.Common.Models;

namespace ProtoMaster.Common
{
    public class CommonData
    {
        public CommonData()
        {
            EgoVehiclePose = new CommonEgoVehiclePose();
            LaneLines = new CommonLaneLineList { LaneLines = new List<CommonLaneLine>() };
            Obstacles = new CommonObstacleList { Obstacles = new List<CommonObstacle>() };
            RoadMarkers = new CommonRoadMarkerList { roadMarkerList = new List<CommonRoadMarker>() };
            HPAData = new CommonHPAData
            {
                HPAPathDetail = new HPAPathDetail
                {
                    CommonParkingSlotList = new CommonParkingSlotList
                    {
                        parkingSlotList = new List<CommonParkingSlot>()
                    }
                }
            };
            TrajectoryPoints = new CommonTrajectoryPoints();
            StateInfo = new CommonStateInfo();
            SlotList = new CommonParkingSlotList { parkingSlotList = new List<CommonParkingSlot>() };
        }

        public CommonEgoVehiclePose EgoVehiclePose { get; set; }
        public CommonLaneLineList LaneLines { get; set; }
        public CommonObstacleList Obstacles { get; set; }
        public CommonRoadMarkerList RoadMarkers { get; set; }
        public CommonHPAData HPAData { get; set; }
        public CommonTrajectoryPoints TrajectoryPoints { get; set; }
        public CommonStateInfo StateInfo { get; set; }
        public CommonParkingSlotList SlotList { get; set; }
    }

    public class Triplet
    {
        public int DataID { get; set; }
        public int DataLength { get; set; }
        public ulong Timestamp { get; set; }
    }

    public class Frame
    {
        public Triplet TripletInfo { get; set; }
        public byte[] Data { get; set; }

        public CommonData CommonData { get; set; }
    }

    public class FileData
    {
        public Dictionary<int, Frame> Frames { get; set; } = new Dictionary<int, Frame>();
    }

    public class EntireProtoData
    {
        public Dictionary<int, FileData> FilesData { get; set; } = new Dictionary<int, FileData>();
    }


    public enum CommonColor
    {
        None = 0,
        Gray,
        White,
        Green,
        Yellow,
        Red,
        Blue,
        RedFlashing,
        Darkgrey,
        Reserved
    }

}
