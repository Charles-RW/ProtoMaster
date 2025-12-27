
namespace ProtoMaster.Common.Models
{
    public class CommonEgoVehiclePose
    {
        public double longitude{ get; set; }        //经度
        public double latitude { get; set; }        //纬度
        public float altitude { get; set; }         //高度
        public float heading { get; set; }          //航向角
        public float vehicleSpeed { get; set; }     //车辆本身车速，单位:km/h
        public float timestampmSec { get; set; }    //时间戳-毫秒
    }
}
