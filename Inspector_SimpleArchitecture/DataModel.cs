using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Inspector_SimpleArchitecture
{
    #region 데이터 모델 (JSON 구조에 맞춤)

    /// <summary>
    /// 테스트 케이스 구성 파일의 전체 구조
    /// </summary>
    public class TestcaseConfig
    {
        public string Model { get; set; }
        public string TestcaseName { get; set; }
        public string Author { get; set; }
        public string Description { get; set; }
        public string LastModifiedDateTime { get; set; }
        public string CandbPath_Can1 { get; set; }
        public string CandbPath_Can2 { get; set; }
        public string CandbPath_Can3 { get; set; }
        public string CandbPath_Can4 { get; set; }
        public string LindbPath_Lin1 { get; set; }
        public string LindbPath_Lin2 { get; set; }
        public List<TestItemConfig> TestItems { get; set; }
        public object GroupItems { get; set; }
        public Dictionary<string, object> TestParameters { get; set; }
    }

    /// <summary>
    /// 테스트 항목 구성
    /// </summary>
    public class TestItemConfig
    {
        public int No { get; set; }
        public string GroupName { get; set; }
        public string TestItemName { get; set; }
        public string SpecMin { get; set; }
        public string SpecMax { get; set; }
        public string Value { get; set; }
        public string ParamMin { get; set; }
        public string ParamMax { get; set; }
        public string Unit { get; set; }
        public int Result { get; set; }
        public string MESCode { get; set; }
        public bool IsUse { get; set; }
        public int DecimalPoint { get; set; }
        public List<TestStepConfig> StepList { get; set; }
        public List<string> MonitoringCANSignalNameList { get; set; }
        public List<string> MonitoringSpecParameterList { get; set; }
    }

    /// <summary>
    /// 테스트 단계 구성
    /// </summary>
    public class TestStepConfig
    {
        public int No { get; set; }
        public string ObjectName { get; set; }
        public int ObjectChannel { get; set; }
        public string Function { get; set; }
        public List<string> Parameters { get; set; }
        public int MoveStep { get; set; }
        public string ReturnValue { get; set; }
        public bool ResultStep { get; set; }
        public string Condition { get; set; }
        public int Comparer { get; set; }
        public string CompareValue1 { get; set; }
        public string CompareValue2 { get; set; }
    }

    /// <summary>
    /// 테스트 결과를 나타내는 열거형
    /// </summary>
    public enum TestResultStatus
    {
        NotTested = 0,
        Pass = 1,
        Fail = 2
    }

    #endregion
}
