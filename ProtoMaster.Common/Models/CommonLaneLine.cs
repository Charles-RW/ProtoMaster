using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace ProtoMaster.Common.Models
{
    /// <summary>
    /// 车道线类型枚举
    /// </summary>
    public enum LaneLineType : uint
    {
        Unknow = 0x0,                       // 未知
        SingleSolid = 0x1,                  // 单实线
        SingleDashed = 0x2,                 // 单虚线
        DoubleDashedSolid = 0x3,            // 双_虚线_实线
        DoubleSolidDashed = 0x4,            // 双_实线_虚线
        DoubleDashedDashed = 0x5,           // 双_虚线_虚线
        DoubleSolidSolid = 0x6,             // 双_实线_实线
        LeftRoadEdge = 0x7,                 // 左侧路沿
        RightRoadEdge = 0x8,                // 右侧路沿
        FishBoneDashed = 0x9,               // 鱼骨线_虚线
        FishBoneSolid = 0xA,                // 鱼骨线_实线
        LeftGuardrail = 0xB,                // 左侧护栏
        RightGuardrail = 0xC,               // 右侧护栏
        LeftGreenLand = 0xD,                // 左侧绿化带
        RightGreenLand = 0xE,               // 右侧绿化带
        Wall = 0xF,                         // 墙体 (原注释中0xF标为LeftGuardrail，根据逻辑修正为墙体)
        DiversionArea = 0x10,               // 导流线/导流区
        ConstructionArea = 0x11,            // 施工区域
        DenseWideDash = 0x12,               // 短虚线
        FishBoneDoubleDashed = 0x13,        // 鱼骨线_双_虚线_虚线
        FishBoneDoubleSolid = 0x14,         // 鱼骨线_双_实线_实线
        FishBoneDoubleDashedSolid = 0x15,   // 鱼骨线_双_虚线_实线
        FishBoneDoubleSolidDashed = 0x16,   // 鱼骨线_双_实线_虚线
        ChangeLane = 0x17                   // 推荐变道中心线
    }

    public class CommonLaneLine
    {
        public UInt32 lineIndex { get; set; }           // 车道线索引
        public UInt64 lineId { get; set; }              // 车道线ID
        public CommonColor lineColor { get; set; }        // 车道线颜色
        public LaneLineType lineType { get; set; }      // 车道线类型

        public float lineEquation_C0 { get; set; }      // 车道线方程C0 m
        public float lineEquation_C1 { get; set; }      // 车道线方程C1 rad
        public float lineEquation_C2 { get; set; }      // 车道线方程C2 1/m
        public float lineEquation_C3 { get; set; }      // 车道线方程C3 1/m^2

        public float laneLineWidth { get; set; }        // 车道线宽度 单位：cm
        public float lineStart_X { get; set; }          // 车道线起点X坐标 单位：cm
        public float lineStart_Y { get; set; }          // 车道线起点Y坐标 单位：cm
        public float lineEnd_X { get; set; }            // 车道线终止点X坐标 单位：cm
        public float lineEnd_Y { get; set; }            // 车道线终止点Y坐标 单位：cm
        public List<Vector3> linePoints { get; set; } = new List<Vector3>(); // 车道线点集合
    }
    public class CommonLaneLineList
    {
        public List<CommonLaneLine> LaneLines { get; set; } = new List<CommonLaneLine>(); // 车道线集合
    }
}
