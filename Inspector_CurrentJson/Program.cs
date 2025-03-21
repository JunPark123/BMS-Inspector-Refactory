using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Linq;

namespace SimplifiedBMSTestFramework
{
    #region 모델 클래스들

    // 전체 테스트 구성을 나타내는 클래스
    public class TestInfoConfig
    {
        public string Model { get; set; }
        public string TestcaseName { get; set; }
        public string Author { get; set; }
        public string Description { get; set; }
        public string LastModifiedDateTime { get; set; }
        public List<TestItemConfig> TestItems { get; set; }
        public Dictionary<string, string> TestParameters { get; set; }
    }

    // 테스트 항목 구성
    public class TestItemConfig
    {
        public int No { get; set; }
        public string GroupName { get; set; }
        public string TestItemName { get; set; }
        public string SpecMin { get; set; }
        public string SpecMax { get; set; }
        public string Value { get; set; }
        public string Unit { get; set; }
        public int Result { get; set; }
        public string MESCode { get; set; }
        public bool IsUse { get; set; }
        public int DecimalPoint { get; set; }
        public List<TestStepConfig> StepList { get; set; }
        public List<string> MonitoringCANSignalNameList { get; set; }
        public List<string> MonitoringSpecParameterList { get; set; }
    }

    // 테스트 단계 구성
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

    // 테스트 결과를 나타내는 열거형
    public enum TestResultStatus
    {
        NotTested = 0,
        Pass = 1,
        Fail = 2
    }

    #endregion

    #region 외부 인터페이스 연동 

    public enum eProcess
    {
        ready,
        run,
        End,
        mes,
        plc,
        error,
    }
    public interface ITestSequence
    {
        string Id { get; }
        string Name { get; }
        TestResult Execute();
    }

    public class SequencDecorator
    {
        eProcess proc = new eProcess();
        ITestSequence seq;

        public SequencDecorator(ITestSequence seq , eProcess CurrProc):base()
        {
            this.seq = seq;
            this.proc = CurrProc;
        }

        public void Excute(TestStepConfig stCfg)
        {
            
        }

       
    }

    #endregion

    #region 테스트 실행 클래스들

    // 테스트 결과 클래스
    public class TestResult
    {
        public int TestItemNo { get; set; }
        public string TestItemName { get; set; }
        public bool Success { get; set; }
        public string Value { get; set; }
        public string ErrorMessage { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public TimeSpan Duration => EndTime - StartTime;
        public List<StepResult> StepResults { get; set; } = new List<StepResult>();
    }

    // 단계별 실행 결과
    public class StepResult
    {
        public int StepNo { get; set; }
        public string ObjectName { get; set; }
        public string Function { get; set; }
        public bool Success { get; set; }
        public string ReturnValue { get; set; }
        public string Message { get; set; }
    }

    // 기본 테스트 아이템 클래스
    public abstract class TestItem
    {
        public int No { get; protected set; }
        public string GroupName { get; protected set; }
        public string TestItemName { get; protected set; }
        public string SpecMin { get; protected set; }
        public string SpecMax { get; protected set; }
        public string Unit { get; protected set; }
        public bool IsUse { get; protected set; }

        protected readonly List<TestStepConfig> StepConfigs;
        protected readonly List<ITestStep> Steps = new List<ITestStep>();
        protected string MeasuredValue;

        public TestItem(TestItemConfig config)
        {
            No = config.No;
            GroupName = config.GroupName;
            TestItemName = config.TestItemName;
            SpecMin = config.SpecMin;
            SpecMax = config.SpecMax;
            Unit = config.Unit;
            IsUse = config.IsUse;
            StepConfigs = config.StepList ?? new List<TestStepConfig>();

            // 테스트 단계 생성
            InitializeSteps();
        }

        protected virtual void InitializeSteps()
        {
            // 각 단계 구성에 맞는 TestStep 객체 생성
            foreach (var stepConfig in StepConfigs)
            {
                ITestStep step = TestStepFactory.CreateStep(stepConfig);
                if (step != null)
                {
                    Steps.Add(step);
                }
            }
        }

        public TestResult RunTestItem()
        {
            TestResult result = new TestResult
            {
                TestItemNo = No,
                TestItemName = TestItemName,
                StartTime = DateTime.Now
            };

            try
            {
                Console.WriteLine($"실행 시작: {TestItemName} (No: {No})");

                // 모든 단계 실행
                foreach (var step in Steps)
                {
                    StepResult stepResult = step.Execute();
                    result.StepResults.Add(stepResult);

                    // 결과를 저장하는 단계인 경우
                    if (step.Config.ResultStep)
                    {
                        MeasuredValue = stepResult.ReturnValue;
                        result.Success = stepResult.Success;
                    }

                    // 단계 실행 실패 시 종료
                    if (!stepResult.Success)
                    {
                        result.Success = false;
                        result.ErrorMessage = $"단계 {step.Config.No} 실행 실패: {stepResult.Message}";
                        break;
                    }
                }

                // 측정값 검증
                if (result.Success != false && !string.IsNullOrEmpty(MeasuredValue))
                {
                    bool verificationResult = VerifyResult();
                    result.Success = verificationResult;
                    result.Value = MeasuredValue;

                    if (!verificationResult)
                    {
                        result.ErrorMessage = $"검증 실패: 측정값 {MeasuredValue}{Unit}이 허용 범위({SpecMin}~{SpecMax}) 밖입니다.";
                    }
                }
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.ErrorMessage = $"예외 발생: {ex.Message}";
                Console.WriteLine($"테스트 오류: {ex.Message}");
            }

            result.EndTime = DateTime.Now;
            Console.WriteLine($"실행 완료: {TestItemName} - {(result.Success ? "성공" : "실패")} ({result.Duration.TotalSeconds:F3}초)");

            return result;
        }

        protected virtual bool VerifyResult()
        {
            // 측정값이 SpecMin과 SpecMax 사이에 있는지 확인
            try
            {                                                             
                if (double.TryParse(MeasuredValue, out double value) &&
                    double.TryParse(SpecMin, out double min) &&
                    double.TryParse(SpecMax, out double max))
                {
                    return value >= min && value <= max;
                }
                else if (MeasuredValue == SpecMin) // 문자열 비교
                {
                    return true;
                }
            }
            catch
            {
                return false;
            }

            return false;
        }
    }

    // 테스트 아이템 팩토리 클래스
    public static class TestItemFactory
    {
        public static TestItem CreateTestItem(TestItemConfig config)
        {
            // 하위 클래스 확장 가능
            return new StandardTestItem(config);
        }
    }

    // 기준이 되는 테스트 아이템 구현
    public class StandardTestItem : TestItem
    {
        public StandardTestItem(TestItemConfig config) : base(config) { }
    }

    public class SpecialTargetItem : TestItem
    {
        public SpecialTargetItem(TestItemConfig config) : base(config)
        {

        }
    }



    // 테스트 단계 인터페이스
    public interface ITestStep
    {
        TestStepConfig Config { get; }
        StepResult Execute();
    }

    // 기본 테스트 단계 구현
    public abstract class BaseTestStep : ITestStep
    {
        public TestStepConfig Config { get; protected set; }

        public BaseTestStep(TestStepConfig config)
        {
            Config = config;
        }

        public virtual StepResult Execute()
        {
            StepResult result = new StepResult
            {
                StepNo = Config.No,
                ObjectName = Config.ObjectName,
                Function = Config.Function,
                Success = false
            };

            try
            {
                Console.WriteLine($"단계 실행: {Config.No}, {Config.ObjectName}.{Config.Function}");

                // 실제 구현은 하위 클래스에서
                string returnValue = ExecuteStep();

                result.ReturnValue = returnValue;
                result.Success = true;

                Console.WriteLine($"단계 완료: {Config.No}, 반환값: {returnValue}");
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Message = ex.Message;
                Console.WriteLine($"단계 오류: {Config.No}, {ex.Message}");
            }

            return result;
        }

        protected abstract string ExecuteStep();
    }

    // 디지털 멀티미터(DMM) 테스트 단계
    public class DMMTestStep : BaseTestStep
    {
        public DMMTestStep(TestStepConfig config) : base(config) { }

        protected override string ExecuteStep()
        {
            // 실제 DMM 장비와 통신하는 코드가 여기에 들어갑니다.
            // 이 예제에서는 시뮬레이션합니다.

            switch (Config.Function.ToLower())
            {
                case "measuredccurrent":
                    // 0.01~0.1 사이의 랜덤 전류값 생성 (A 단위)
                    double current = 0.01 + new Random().NextDouble() * 0.09;
                    return current.ToString("F4");

                case "measuredcvoltage":
                    // 3.0~3.7 사이의 랜덤 전압값 생성 (V 단위)
                    double voltage = 3.0 + new Random().NextDouble() * 0.7;
                    return voltage.ToString("F4");

                default:
                    throw new NotImplementedException($"지원하지 않는 DMM 함수: {Config.Function}");
            }
        }
    }

    // CAN 통신 관련 테스트 단계
    public class CANTestStep : BaseTestStep
    {
        public CANTestStep(TestStepConfig config) : base(config) { }

        protected override string ExecuteStep()
        {
            // 실제 CAN 인터페이스와 통신하는 코드가 여기에 들어갑니다.
            // 이 예제에서는 시뮬레이션합니다.

            switch (Config.Function.ToLower())
            {
                case "sendmessage":
                    // CAN 메시지 전송 시뮬레이션
                    string messageId = Config.Parameters[0];
                    Thread.Sleep(50); // 통신 지연 시뮬레이션
                    return "true";

                case "readvalue":
                    // CAN 값 읽기 시뮬레이션
                    string signalName = Config.Parameters[0];
                    double value = 10.0 + new Random().NextDouble() * 5.0;
                    return value.ToString("F2");

                default:
                    throw new NotImplementedException($"지원하지 않는 CAN 함수: {Config.Function}");
            }
        }
    }

    // 릴레이 제어 관련 테스트 단계
    public class RelayTestStep : BaseTestStep
    {
        public RelayTestStep(TestStepConfig config) : base(config) { }

        protected override string ExecuteStep()
        {
            // 실제 릴레이 제어 코드가 여기에 들어갑니다.
            // 이 예제에서는 시뮬레이션합니다.

            switch (Config.Function.ToLower())
            {
                case "set":
                    int relayNumber = int.Parse(Config.Parameters[0]);
                    bool state = bool.Parse(Config.Parameters[1]);
                    Console.WriteLine($"릴레이 {relayNumber} {(state ? "ON" : "OFF")} 설정");
                    Thread.Sleep(20); // 릴레이 동작 시간 시뮬레이션
                    return "true";

                default:
                    throw new NotImplementedException($"지원하지 않는 릴레이 함수: {Config.Function}");
            }
        }
    }

    // 테스트 단계 팩토리 클래스
    public static class TestStepFactory
    {
        public static ITestStep CreateStep(TestStepConfig config)
        {
            switch (config.ObjectName.ToUpper())
            {
                case "DMM":
                    return new DMMTestStep(config);

                case "CAN":
                    return new CANTestStep(config);

                case "RELAY":
                    return new RelayTestStep(config);

                default:
                    Console.WriteLine($"경고: 지원하지 않는 장치 유형 '{config.ObjectName}'");
                    return null;
            }
        }
    }


    // 테스트 관리자 클래스
    public class TestManager
    {
        private readonly List<TestResult> _results = new List<TestResult>();

        // JSON 파일에서 테스트 구성 로드
        public TestInfoConfig LoadTestSuite(string jsonFilePath)
        {
            Console.WriteLine($"테스트 스위트 로드 중: {jsonFilePath}");

            string jsonContent = File.ReadAllText(jsonFilePath);
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            return JsonSerializer.Deserialize<TestInfoConfig>(jsonContent, options);
        }

        // 테스트 실행 (순차)
        public void RunSequentially(TestInfoConfig testSuite)
        {
            Console.WriteLine($"===== 순차 테스트 실행 시작 ({testSuite.TestItems.Count(x => x.IsUse)}개 테스트) =====");
            _results.Clear();

            foreach (var itemConfig in testSuite.TestItems.Where(x => x.IsUse))
            {
                TestItem testItem = TestItemFactory.CreateTestItem(itemConfig);
                TestResult result = testItem.RunTestItem();
                _results.Add(result);

                // 필요에 따라 실패 시 중단 로직
                if (!result.Success && false) // "false" 를 실제 중단 조건으로 변경
                {
                    Console.WriteLine($"테스트 실패로 인해 중단: {result.TestItemName}");
                    break;
                }
            }

            Console.WriteLine("===== 순차 테스트 실행 완료 =====");
        }

        //MES 업로드
        //테스트 스텝 진행중 에러 처리
        //테스트 스텝에서 Result시 Fail시 에러처리
        //Fail시 램프 색상
        //PLC랑 연동해야 할 때??
        //

        // 테스트 결과 가져오기
        public List<TestResult> GetResults()
        {
            return _results;
        }

        // 콘솔 결과 보고서 생성
        public void GenerateConsoleReport()
        {
            if (_results.Count == 0)
            {
                Console.WriteLine("테스트 결과가 없습니다.");
                return;
            }

            Console.WriteLine("\n===== 테스트 결과 보고서 =====");
            Console.WriteLine($"총 테스트 수: {_results.Count}");

            int passCount = _results.Count(r => r.Success);
            int failCount = _results.Count - passCount;

            Console.WriteLine($"성공: {passCount} ({(passCount * 100.0 / _results.Count):F1}%)");
            Console.WriteLine($"실패: {failCount} ({(failCount * 100.0 / _results.Count):F1}%)");

            TimeSpan totalDuration = TimeSpan.FromTicks(_results.Sum(r => r.Duration.Ticks));
            Console.WriteLine($"총 소요 시간: {totalDuration.TotalSeconds:F3}초");

            Console.WriteLine("\n----- 개별 테스트 결과 -----");
            foreach (var result in _results)
            {
                Console.WriteLine($"[{(result.Success ? "성공" : "실패")}] {result.TestItemName} ({result.Duration.TotalSeconds:F3}초)");
                Console.WriteLine($"  측정값: {result.Value}");

                if (!result.Success)
                {
                    Console.WriteLine($"  오류: {result.ErrorMessage}");
                }

                foreach (var stepResult in result.StepResults)
                {
                    Console.WriteLine($"  - 단계 {stepResult.StepNo}: {(stepResult.Success ? "성공" : "실패")} - {stepResult.ObjectName}.{stepResult.Function} => {stepResult.ReturnValue}");
                }

                Console.WriteLine();
            }

            Console.WriteLine("===== 보고서 종료 =====\n");
        }

        // JSON 결과 보고서 생성
        public void GenerateJsonReport(string outputFilePath)
        {
            var report = new
            {
                TotalTests = _results.Count,
                PassCount = _results.Count(r => r.Success),
                FailCount = _results.Count(r => !r.Success),
                TotalDuration = TimeSpan.FromTicks(_results.Sum(r => r.Duration.Ticks)).TotalSeconds,
                TestResults = _results
            };

            string jsonReport = JsonSerializer.Serialize(report, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            File.WriteAllText(outputFilePath, jsonReport);
            Console.WriteLine($"JSON 보고서 생성 완료: {outputFilePath}");
        }
    }

    #endregion

    #region 프로그램 실행 예제

    public class Program
    {
        public static void Main(string[] args)
        {
            // TestCase 불러오기
            CreateSampleTestFile("bms_test_config.json");

            // TestManager
            var testManager = new TestManager();

            // TestCase 데이터 포맷에 맞게 역직렬화하기
            var testSuite = testManager.LoadTestSuite("bms_test_config.json");

            Console.WriteLine("\n테스트 실행 중...");
            // 테스트 실행
            testManager.RunSequentially(testSuite);

            // 결과 출력
            testManager.GenerateConsoleReport();
            testManager.GenerateJsonReport("test_results.json");

            Console.WriteLine("\n모든 테스트 완료. 아무 키나 누르면 종료됩니다.");
            Console.ReadLine();
        }

        private static void CreateSampleTestFile(string filePath)
        {
            // 제공된 JSON 테스트 구성을 파일로 저장
            string json = @"{
                                ""Model"": ""TEST"",
                                ""TestcaseName"": ""Sample Testcase"",
                                ""Author"": ""GreenPower"",
                                ""Description"": ""This test case was created for sample"",
                                ""LastModifiedDateTime"": ""2025-01-06 17:44:34"",
                                ""CandbPath_Can1"": """",
                                ""CandbPath_Can2"": """",
                                ""CandbPath_Can3"": """",
                                ""CandbPath_Can4"": """",
                                ""LindbPath_Lin1"": """",
                                ""LindbPath_Lin2"": """",
                                ""TestItems"": 
                                            [
                                                {
                                                  ""No"": 1,
                                                  ""GroupName"": ""BMS Test"",
                                                  ""TestItemName"": ""BMS Current Meas1"",
                                                  ""SpecMin"": ""0.01"",
                                                  ""SpecMax"": ""0.1"",
                                                  ""Value"": """",
                                                  ""ParamMin"": """",
                                                  ""ParamMax"": """",
                                                  ""Unit"": ""A"",
                                                  ""Result"": 0,
                                                  ""MESCode"": """",
                                                  ""IsUse"": true,
                                                  ""DecimalPoint"": 0,
                                                  ""StepList"": [
                                                    {
                                                      ""No"": 1,
                                                      ""ObjectName"": ""DMM"",
                                                      ""ObjectChannel"": 1,
                                                      ""Function"": ""MeasureDCCurrent"",
                                                      ""Parameters"": [
                                                        ""0""
                                                      ],
                                                      ""MoveStep"": 0,
                                                      ""ReturnValue"": null,
                                                      ""ResultStep"": true,
                                                      ""Condition"": null,
                                                      ""Comparer"": 0,
                                                      ""CompareValue1"": null,
                                                      ""CompareValue2"": null
                                                    },		
                                                    {
                                                      ""No"": 2,
                                                      ""ObjectName"": ""RELAY"",
                                                      ""ObjectChannel"": 1,
                                                      ""Function"": ""set"",
                                                      ""Parameters"": [
                                                        ""1"",
                                                        ""true""
                                                      ],
                                                      ""MoveStep"": 0,
                                                      ""ReturnValue"": null,
                                                      ""ResultStep"": false,
                                                      ""Condition"": null,
                                                      ""Comparer"": 0,
                                                      ""CompareValue1"": null,
                                                      ""CompareValue2"": null
                                                    },
                                                    {
                                                      ""No"": 3,
                                                      ""ObjectName"": ""DMM"",
                                                      ""ObjectChannel"": 1,
                                                      ""Function"": ""MeasureDCCurrent"",
                                                      ""Parameters"": [
                                                        ""1"",
                                                        ""true""
                                                      ],
                                                      ""MoveStep"": 0,
                                                      ""ReturnValue"": null,
                                                      ""ResultStep"": true,
                                                      ""Condition"": null,
                                                      ""Comparer"": 0,
                                                      ""CompareValue1"": null,
                                                      ""CompareValue2"": null
                                                    }
                                                  ],
                                                  ""MonitoringCANSignalNameList"": [],
                                                  ""MonitoringSpecParameterList"": []
                                                },
                                                {
                                                  ""No"": 2,
                                                  ""GroupName"": ""BMS Test"",
                                                  ""TestItemName"": ""BMS Current Meas2"",
                                                  ""SpecMin"": ""0.02"",
                                                  ""SpecMax"": ""0.1"",
                                                  ""Value"": """",
                                                  ""ParamMin"": """",
                                                  ""ParamMax"": """",
                                                  ""Unit"": ""A"",
                                                  ""Result"": 0,
                                                  ""MESCode"": """",
                                                  ""IsUse"": true,
                                                  ""DecimalPoint"": 0,
                                                  ""StepList"": [
                                                    {
                                                      ""No"": 1,
                                                      ""ObjectName"": ""DMM"",
                                                      ""ObjectChannel"": 1,
                                                      ""Function"": ""MeasureDCCurrent"",
                                                      ""Parameters"": [
                                                        ""0""
                                                      ],
                                                      ""MoveStep"": 0,
                                                      ""ReturnValue"": null,
                                                      ""ResultStep"": true,
                                                      ""Condition"": null,
                                                      ""Comparer"": 0,
                                                      ""CompareValue1"": null,
                                                      ""CompareValue2"": null
                                                    }
                                                  ],
                                                  ""MonitoringCANSignalNameList"": [],
                                                  ""MonitoringSpecParameterList"": []
                                                },
                                                {
                                                  ""No"": 3,
                                                  ""GroupName"": ""BMS Test"",
                                                  ""TestItemName"": ""BMS Current Meas3"",
                                                  ""SpecMin"": ""0.03"",
                                                  ""SpecMax"": ""0.1"",
                                                  ""Value"": """",
                                                  ""ParamMin"": """",
                                                  ""ParamMax"": """",
                                                  ""Unit"": ""A"",
                                                  ""Result"": 0,
                                                  ""MESCode"": """",
                                                  ""IsUse"": true,
                                                  ""DecimalPoint"": 0,
                                                  ""StepList"": [
                                                    {
                                                      ""No"": 1,
                                                      ""ObjectName"": ""DMM"",
                                                      ""ObjectChannel"": 1,
                                                      ""Function"": ""MeasureDCCurrent"",
                                                      ""Parameters"": [
                                                        ""0""
                                                      ],
                                                      ""MoveStep"": 0,
                                                      ""ReturnValue"": null,
                                                      ""ResultStep"": true,
                                                      ""Condition"": null,
                                                      ""Comparer"": 0,
                                                      ""CompareValue1"": null,
                                                      ""CompareValue2"": null
                                                    }
                                                  ],
                                                  ""MonitoringCANSignalNameList"": [],
                                                  ""MonitoringSpecParameterList"": []
                                                },
                                                {
                                                  ""No"": 4,
                                                  ""GroupName"": ""BMS Test"",
                                                  ""TestItemName"": ""BMS Current Meas4"",
                                                  ""SpecMin"": ""0.04"",
                                                  ""SpecMax"": ""0.1"",
                                                  ""Value"": """",
                                                  ""ParamMin"": """",
                                                  ""ParamMax"": """",
                                                  ""Unit"": ""A"",
                                                  ""Result"": 0,
                                                  ""MESCode"": """",
                                                  ""IsUse"": true,
                                                  ""DecimalPoint"": 0,
                                                  ""StepList"": [
                                                    {
                                                      ""No"": 1,
                                                      ""ObjectName"": ""DMM"",
                                                      ""ObjectChannel"": 1,
                                                      ""Function"": ""MeasureDCCurrent"",
                                                      ""Parameters"": [
                                                        ""0""
                                                      ],
                                                      ""MoveStep"": 0,
                                                      ""ReturnValue"": null,
                                                      ""ResultStep"": true,
                                                      ""Condition"": null,
                                                      ""Comparer"": 0,
                                                      ""CompareValue1"": null,
                                                      ""CompareValue2"": null
                                                    }
                                                  ],
                                                  ""MonitoringCANSignalNameList"": [],
                                                  ""MonitoringSpecParameterList"": []
                                                },
                                                {
                                                  ""No"": 5,
                                                  ""GroupName"": ""BMS Test"",
                                                  ""TestItemName"": ""BMS Current Meas6"",
                                                  ""SpecMin"": ""0.05"",
                                                  ""SpecMax"": ""0.1"",
                                                  ""Value"": """",
                                                  ""ParamMin"": """",
                                                  ""ParamMax"": """",
                                                  ""Unit"": ""A"",
                                                  ""Result"": 0,
                                                  ""MESCode"": """",
                                                  ""IsUse"": true,
                                                  ""DecimalPoint"": 0,
                                                  ""StepList"": [
                                                    {
                                                      ""No"": 1,
                                                      ""ObjectName"": ""DMM"",
                                                      ""ObjectChannel"": 1,
                                                      ""Function"": ""MeasureDCCurrent"",
                                                      ""Parameters"": [
                                                        ""0""
                                                      ],
                                                      ""MoveStep"": 0,
                                                      ""ReturnValue"": null,
                                                      ""ResultStep"": true,
                                                      ""Condition"": null,
                                                      ""Comparer"": 0,
                                                      ""CompareValue1"": null,
                                                      ""CompareValue2"": null
                                                    }
                                                  ],
                                                  ""MonitoringCANSignalNameList"": [],
                                                  ""MonitoringSpecParameterList"": []
                                                }
                                              ],
                                              ""GroupItems"": null,
                                              ""TestParameters"": {}
                                            }";

            File.WriteAllText(filePath, json);
            Console.WriteLine($"샘플 테스트 구성 파일 생성 완료: {filePath}");
        }
    }

    #endregion
}