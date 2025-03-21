using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using System.Diagnostics;

namespace BMSTestFramework
{
    #region 핵심 인터페이스와 추상 클래스

    /// <summary>
    /// 테스트 케이스의 기본 인터페이스
    /// </summary>
    public interface ITestCase
    {
        string Id { get; }
        string Name { get; }
        TestResult Execute();
    }

    /// <summary>
    /// 템플릿 메소드 패턴을 적용한 추상 테스트 케이스
    /// </summary>
    public abstract class AbstractTestCase : ITestCase
    {
        public string Id { get; protected set; }
        public string Name { get; protected set; }
        public Dictionary<string, object> Parameters { get; protected set; }
        public TestStrategy Strategy { get; protected set; }

        protected AbstractTestCase(string id, string name, Dictionary<string, object> parameters, TestStrategy strategy)
        {
            Id = id;
            Name = name;
            Parameters = parameters;
            Strategy = strategy;
        }

        // 템플릿 메소드 - 테스트 실행 흐름 정의
        public TestResult Execute()
        {
            TestResult result = new TestResult(Id, Name);
            result.StartTime = DateTime.Now;

            try
            {
                Console.WriteLine($"테스트 시작: {Name} (ID: {Id})");

                // 1단계: 초기화
                Initialize();
                result.Steps.Add(new TestStep("초기화", true, "초기화 완료"));

                // 2단계: 실행
                Strategy.RunTest(Parameters);
                result.Steps.Add(new TestStep("실행", true, "실행 완료"));

                // 3단계: 검증
                bool verificationResult = Verify();
                result.Steps.Add(new TestStep("검증", verificationResult, verificationResult ? "검증 성공" : "검증 실패"));

                result.Success = verificationResult;
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Steps.Add(new TestStep("오류", false, ex.Message));
                result.ErrorMessage = ex.Message;
                Console.WriteLine($"테스트 오류: {ex.Message}");
            }
            finally
            {
                // 4단계: 정리
                try
                {
                    Cleanup();
                    result.Steps.Add(new TestStep("정리", true, "정리 완료"));
                }
                catch (Exception ex)
                {
                    result.Steps.Add(new TestStep("정리 오류", false, ex.Message));
                    Console.WriteLine($"정리 오류: {ex.Message}");
                }
            }

            result.EndTime = DateTime.Now;
            result.Duration = result.EndTime - result.StartTime;

            Console.WriteLine($"테스트 종료: {Name} - {(result.Success ? "성공" : "실패")} ({result.Duration.TotalSeconds}초)");

            return result;
        }

        // 하위 클래스에서 구현할 추상 메소드들
        protected abstract void Initialize();
        protected abstract bool Verify();
        protected abstract void Cleanup();
    }

    /// <summary>
    /// 테스트 전략 인터페이스 (전략 패턴)
    /// </summary>
    public abstract class TestStrategy
    {
        public abstract void RunTest(Dictionary<string, object> parameters);
    }

    /// <summary>
    /// 테스트 데코레이터 (데코레이터 패턴)
    /// </summary>
    public abstract class TestDecorator : ITestCase
    {
        protected ITestCase TestCase;

        public TestDecorator(ITestCase testCase)
        {
            TestCase = testCase;
        }

        public string Id => TestCase.Id;
        public string Name => TestCase.Name;

        public virtual TestResult Execute()
        {
            return TestCase.Execute();
        }
    }

    #endregion

    #region 테스트 전략 구현체

    /// <summary>
    /// 전압 테스트 전략
    /// </summary>
    public class VoltageTestStrategy : TestStrategy
    {
        public override void RunTest(Dictionary<string, object> parameters)
        {
            Console.WriteLine("전압 테스트 실행 중...");

            // 테스트 파라미터 추출
            double targetVoltage = parameters.GetValue<double>("targetVoltage");
            int durationMs = parameters.GetValue<int>("durationMs");

            // 실제 하드웨어 제어 로직 (예시)
            Console.WriteLine($"  대상 전압: {targetVoltage}V, 지속 시간: {durationMs}ms");
            Console.WriteLine("  전압 출력 설정...");
            Thread.Sleep(500); // 하드웨어 설정 지연 시뮬레이션

            Console.WriteLine("  전압 측정 중...");
            Thread.Sleep(durationMs); // 테스트 지속 시간 시뮬레이션

            // 측정 결과 저장 (실제로는 하드웨어에서 읽어옴)
            parameters["measuredVoltage"] = targetVoltage + (new Random().NextDouble() * 0.2 - 0.1);

            Console.WriteLine($"  측정된 전압: {parameters["measuredVoltage"]}V");
        }
    }

    /// <summary>
    /// 전류 테스트 전략
    /// </summary>
    public class CurrentTestStrategy : TestStrategy
    {
        public override void RunTest(Dictionary<string, object> parameters)
        {
            Console.WriteLine("전류 테스트 실행 중...");

            // 테스트 파라미터 추출
            double targetCurrent = parameters.GetValue<double>("targetCurrent");
            int durationMs = parameters.GetValue<Int32>("durationMs");

            // 실제 하드웨어 제어 로직 (예시)
            Console.WriteLine($"  대상 전류: {targetCurrent}A, 지속 시간: {durationMs}ms");
            Console.WriteLine("  전류 출력 설정...");
            Thread.Sleep(500); // 하드웨어 설정 지연 시뮬레이션

            Console.WriteLine("  전류 측정 중...");
            Thread.Sleep(durationMs); // 테스트 지속 시간 시뮬레이션

            // 측정 결과 저장 (실제로는 하드웨어에서 읽어옴)
            parameters["measuredCurrent"] = targetCurrent + (new Random().NextDouble() * 0.2 - 0.1);

            Console.WriteLine($"  측정된 전류: {parameters["measuredCurrent"]}A");
        }
    }

    /// <summary>
    /// CAN 통신 테스트 전략
    /// </summary>
    public class CANCommTestStrategy : TestStrategy
    {
        public override void RunTest(Dictionary<string, object> parameters)
        {
            Console.WriteLine("CAN 통신 테스트 실행 중...");

            // 테스트 파라미터 추출
            string canId = parameters.GetValue<string>("canID");
            string messageData = parameters.GetValue<string>("messageData");
            int timeoutMs = parameters.GetValue<Int32>("timeoutMs");

            // 실제 하드웨어 제어 로직 (예시)
            Console.WriteLine($"  CAN ID: {canId}, 메시지: {messageData}, 타임아웃: {timeoutMs}ms");
            Console.WriteLine("  CAN 메시지 전송 중...");
            Thread.Sleep(100); // 메시지 전송 지연 시뮬레이션

            // 응답 대기 (시뮬레이션)
            Console.WriteLine("  응답 대기 중...");
            Thread.Sleep(new Random().Next(50, timeoutMs));

            // 응답 결과 저장 (실제로는 하드웨어에서 읽어옴)
            bool responseReceived = new Random().NextDouble() > 0.1; // 90% 성공 확률
            parameters["responseReceived"] = responseReceived;

            if (responseReceived)
            {
                parameters["responseData"] = "53 4F 43 3A 38 35"; // 예시 응답 데이터
                Console.WriteLine($"  응답 수신: {parameters["responseData"]}");
            }
            else
            {
                Console.WriteLine("  응답 수신 실패");
            }
        }
    }

    #endregion

    #region 구체적인 테스트 케이스 구현

    /// <summary>
    /// 전압 테스트 케이스
    /// </summary>
    public class VoltageTestCase : AbstractTestCase
    {
        private readonly double _tolerance;

        public VoltageTestCase(string id, string name, Dictionary<string, object> parameters)
            : base(id, name, parameters, new VoltageTestStrategy())
        {
            _tolerance = parameters.ContainsKey("tolerance")? 
                (parameters["tolerance"] is JsonElement jsonElement ? jsonElement.GetDouble() : Convert.ToDouble(parameters["tolerance"]))
    : 0.1;
        }

        protected override void Initialize()
        {
            Console.WriteLine("전압 테스트 초기화 중...");
            // 하드웨어 초기화 로직
            Thread.Sleep(200); // 초기화 지연 시뮬레이션
        }

        protected override bool Verify()
        {
            double targetVoltage = Parameters.GetValue<double>("targetVoltage");
            double measuredVoltage = Parameters.GetValue<double>("measuredVoltage");

            // 측정값이 허용 범위 내에 있는지 확인
            bool result = Math.Abs(targetVoltage - measuredVoltage) <= _tolerance;

            Console.WriteLine($"전압 검증 결과: {(result ? "통과" : "실패")}");
            Console.WriteLine($"  대상 전압: {targetVoltage}V, 측정 전압: {measuredVoltage}V, 허용 오차: {_tolerance}V");

            return result;
        }

        protected override void Cleanup()
        {
            Console.WriteLine("전압 테스트 정리 중...");
            // 하드웨어 정리 로직
            Thread.Sleep(100); // 정리 지연 시뮬레이션
        }
    }

    /// <summary>
    /// 전류 테스트 케이스
    /// </summary>
    public class CurrentTestCase : AbstractTestCase
    {
        private readonly double _tolerance;

        public CurrentTestCase(string id, string name, Dictionary<string, object> parameters)
            : base(id, name, parameters, new CurrentTestStrategy())
        {
            _tolerance = parameters.ContainsKey("tolerance") ? parameters.GetValue<double>("tolerance") : 0.05;
        }

        protected override void Initialize()
        {
            Console.WriteLine("전류 테스트 초기화 중...");
            // 하드웨어 초기화 로직
            Thread.Sleep(200); // 초기화 지연 시뮬레이션
        }

        protected override bool Verify()
        {
            double targetCurrent = Parameters.GetValue<double>("targetCurrent");
            double measuredCurrent = Parameters.GetValue<double>("measuredCurrent");

            // 측정값이 허용 범위 내에 있는지 확인
            bool result = Math.Abs(targetCurrent - measuredCurrent) <= _tolerance;

            Console.WriteLine($"전류 검증 결과: {(result ? "통과" : "실패")}");
            Console.WriteLine($"  대상 전류: {targetCurrent}A, 측정 전류: {measuredCurrent}A, 허용 오차: {_tolerance}A");

            return result;
        }

        protected override void Cleanup()
        {
            Console.WriteLine("전류 테스트 정리 중...");
            // 하드웨어 정리 로직
            Thread.Sleep(100); // 정리 지연 시뮬레이션
        }
    }

    /// <summary>
    /// CAN 통신 테스트 케이스
    /// </summary>
    public class CANCommTestCase : AbstractTestCase
    {
        public CANCommTestCase(string id, string name, Dictionary<string, object> parameters)
            : base(id, name, parameters, new CANCommTestStrategy())
        {
        }

        protected override void Initialize()
        {
            Console.WriteLine("CAN 통신 테스트 초기화 중...");
            // CAN 인터페이스 초기화 로직
            Thread.Sleep(300); // 초기화 지연 시뮬레이션
        }

        protected override bool Verify()
        {
            // 응답 수신 여부 확인
            bool responseReceived = Parameters.GetValue<bool>("responseReceived");

            if (!responseReceived)
            {
                Console.WriteLine("CAN 통신 검증 실패: 응답 수신되지 않음");
                return false;
            }

            // 응답 데이터 검증 (실제 구현에서는 더 복잡한 검증 로직이 필요할 수 있음)
            string responseData = Parameters.GetValue<string>("responseData");
            bool validResponse = !string.IsNullOrEmpty(responseData);

            Console.WriteLine($"CAN 통신 검증 결과: {(validResponse ? "통과" : "실패")}");
            if (validResponse)
            {
                Console.WriteLine($"  수신된 응답: {responseData}");
            }

            return validResponse;
        }

        protected override void Cleanup()
        {
            Console.WriteLine("CAN 통신 테스트 정리 중...");
            // CAN 인터페이스 정리 로직
            Thread.Sleep(100); // 정리 지연 시뮬레이션
        }
    }

    #endregion

    #region 데코레이터 구현체

    /// <summary>
    /// 타이밍 측정 데코레이터
    /// </summary>
    public class TimingDecorator : TestDecorator
    {
        public TimingDecorator(ITestCase testCase) : base(testCase)
        {
        }

        public override TestResult Execute()
        {
            Stopwatch stopwatch = new Stopwatch();
            Console.WriteLine($"[타이밍] '{TestCase.Name}' 테스트 시작");

            stopwatch.Start();
            TestResult result = base.Execute();
            stopwatch.Stop();

            Console.WriteLine($"[타이밍] '{TestCase.Name}' 테스트 완료: {stopwatch.ElapsedMilliseconds}ms");

            return result;
        }
    }

    /// <summary>
    /// 자동 재시도 데코레이터
    /// </summary>
    public class RetryDecorator : TestDecorator
    {
        private readonly int _maxRetries;
        private readonly int _delayMs;

        public RetryDecorator(ITestCase testCase, int maxRetries = 3, int delayMs = 1000) : base(testCase)
        {
            _maxRetries = maxRetries;
            _delayMs = delayMs;
        }

        public override TestResult Execute()
        {
            TestResult result = null;
            int attempts = 0;

            do
            {
                attempts++;
                Console.WriteLine($"[재시도] '{TestCase.Name}' 테스트 실행 (시도 {attempts}/{_maxRetries + 1})");

                result = base.Execute();

                if (result.Success)
                {
                    break;
                }

                if (attempts <= _maxRetries)
                {
                    Console.WriteLine($"[재시도] '{TestCase.Name}' 테스트 실패, {_delayMs}ms 후 재시도...");
                    Thread.Sleep(_delayMs);
                }

            } while (attempts <= _maxRetries);

            if (!result.Success)
            {
                Console.WriteLine($"[재시도] '{TestCase.Name}' 테스트 최종 실패 ({attempts} 시도)");
            }
            else if (attempts > 1)
            {
                Console.WriteLine($"[재시도] '{TestCase.Name}' 테스트 {attempts}번째 시도에서 성공");
            }

            return result;
        }
    }

    #endregion

    #region 테스트 프레임워크 핵심 컴포넌트

    /// <summary>
    /// 테스트 로더 - JSON 파일에서 테스트 케이스 로드
    /// </summary>
    public class TestLoader
    {
        public List<TestCase> LoadTestsFromJson(string jsonFilePath)
        {
            Console.WriteLine($"JSON 파일에서 테스트 케이스 로드 중: {jsonFilePath}");

            string jsonContent = File.ReadAllText(jsonFilePath);
            var testSuite = JsonSerializer.Deserialize<TestSuite>(jsonContent);

            List<TestCase> testCases = new List<TestCase>();

            foreach (var test in testSuite.Tests)
            {
                testCases.Add(test);
            }

            Console.WriteLine($"테스트 케이스 {testCases.Count}개 로드 완료");

            return testCases;
        }
    }

    /// <summary>
    /// 테스트 케이스 인터프리터 - 테스트 케이스 생성 및 해석
    /// </summary>
    public class TestCaseInterpreter
    {
        public ITestCase InterpretTestCase(TestCase testCase)
        {
            Console.WriteLine($"테스트 케이스 해석 중: {testCase.Name} (타입: {testCase.Type})");

            ITestCase test = null;

            // 테스트 타입에 따라 적절한 테스트 케이스 객체 생성
            switch (testCase.Type.ToLower())
            {
                case "voltage":
                    test = new VoltageTestCase(testCase.Id, testCase.Name, testCase.Parameters);
                    break;

                case "current":
                    test = new CurrentTestCase(testCase.Id, testCase.Name, testCase.Parameters);
                    break;

                case "cancomm":
                    test = new CANCommTestCase(testCase.Id, testCase.Name, testCase.Parameters);
                    break;

                default:
                    throw new ArgumentException($"지원되지 않는 테스트 타입: {testCase.Type}");
            }

            // 데코레이터 적용
            if (testCase.EnableRetry)
            {
                int maxRetries = testCase.Parameters.GetValue<int>("maxRetries", 3);
                test = new RetryDecorator(test, maxRetries);
            }

            // 항상 타이밍 데코레이터 적용
            test = new TimingDecorator(test);

            return test;
        }
    }

    /// <summary>
    /// 테스트 스케줄러 - 테스트 케이스 실행 관리
    /// </summary>
    public class TestScheduler
    {
        private readonly TestCaseInterpreter _interpreter;
        private readonly TestResultCollector _resultCollector;

        public TestScheduler(TestCaseInterpreter interpreter, TestResultCollector resultCollector)
        {
            _interpreter = interpreter;
            _resultCollector = resultCollector;
        }

        public void RunSequentially(List<TestCase> testCases)
        {
            Console.WriteLine($"====== 순차 테스트 실행 시작 ({testCases.Count}개 테스트) ======");

            foreach (var testCase in testCases)
            {
                ITestCase test = _interpreter.InterpretTestCase(testCase);
                TestResult result = test.Execute();
                _resultCollector.AddResult(result);
            }

            Console.WriteLine("====== 순차 테스트 실행 완료 ======");
        }

        public async Task RunParallelAsync(List<TestCase> testCases, int maxParallelism = 4)
        {
            Console.WriteLine($"====== 병렬 테스트 실행 시작 ({testCases.Count}개 테스트, 최대 병렬 수: {maxParallelism}) ======");

            // 병렬 처리를 위한 작업 리스트 생성
            var tasks = testCases.Select(async testCase =>
            {
                ITestCase test = _interpreter.InterpretTestCase(testCase);
                TestResult result = await Task.Run(() => test.Execute());
                _resultCollector.AddResult(result);
            }).ToList();

            // 병렬 처리 시작
            var chunkedTasks = tasks.Chunk(maxParallelism).ToList();
            foreach (var chunk in chunkedTasks)
            {
                await Task.WhenAll(chunk);
            }

            Console.WriteLine("====== 병렬 테스트 실행 완료 ======");
        }
    }

    /// <summary>
    /// 테스트 결과 수집기
    /// </summary>
    public class TestResultCollector
    {
        private readonly List<TestResult> _results = new List<TestResult>();

        public void AddResult(TestResult result)
        {
            lock (_results)
            {
                _results.Add(result);
                Console.WriteLine($"테스트 결과 추가: {result.TestName} - {(result.Success ? "성공" : "실패")}");
            }
        }

        public List<TestResult> GetResults()
        {
            return _results;
        }

        public void Clear()
        {
            lock (_results)
            {
                _results.Clear();
                Console.WriteLine("테스트 결과 초기화");
            }
        }
    }

    /// <summary>
    /// 보고서 생성기
    /// </summary>
    public class ReportGenerator
    {
        public void GenerateConsoleReport(List<TestResult> results)
        {
            Console.WriteLine("\n====== 테스트 결과 보고서 ======");
            Console.WriteLine($"총 테스트 수: {results.Count}");

            int passCount = results.Count(r => r.Success);
            int failCount = results.Count - passCount;

            Console.WriteLine($"성공: {passCount} ({(passCount * 100.0 / results.Count):F1}%)");
            Console.WriteLine($"실패: {failCount} ({(failCount * 100.0 / results.Count):F1}%)");

            TimeSpan totalDuration = TimeSpan.FromTicks(results.Sum(r => r.Duration.Ticks));
            Console.WriteLine($"총 소요 시간: {totalDuration.TotalSeconds:F3}초");

            Console.WriteLine("\n----- 개별 테스트 결과 -----");
            foreach (var result in results)
            {
                Console.WriteLine($"[{(result.Success ? "성공" : "실패")}] {result.TestName} ({result.Duration.TotalSeconds:F3}초)");

                if (!result.Success)
                {
                    Console.WriteLine($"  오류: {result.ErrorMessage}");
                }

                foreach (var step in result.Steps)
                {
                    Console.WriteLine($"  - {step.Name}: {(step.Success ? "성공" : "실패")} - {step.Message}");
                }

                Console.WriteLine();
            }

            Console.WriteLine("====== 보고서 종료 ======\n");
        }

        public void GenerateJsonReport(List<TestResult> results, string outputFilePath)
        {
            var report = new
            {
                TotalTests = results.Count,
                PassCount = results.Count(r => r.Success),
                FailCount = results.Count(r => !r.Success),
                TotalDuration = TimeSpan.FromTicks(results.Sum(r => r.Duration.Ticks)).TotalSeconds,
                TestResults = results
            };

            string jsonReport = JsonSerializer.Serialize(report, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            File.WriteAllText(outputFilePath, jsonReport);
            Console.WriteLine($"JSON 보고서 생성 완료: {outputFilePath}");
        }
    }

    /// <summary>
    /// 테스트 실행 엔진 - 전체 테스트 프로세스 조정
    /// </summary>
    public class TestExecutionEngine
    {
        private readonly TestLoader _loader;
        private readonly TestCaseInterpreter _interpreter;
        private readonly TestResultCollector _resultCollector;
        private readonly TestScheduler _scheduler;
        private readonly ReportGenerator _reportGenerator;

        public TestExecutionEngine()
        {
            _loader = new TestLoader();
            _resultCollector = new TestResultCollector();
            _interpreter = new TestCaseInterpreter();
            _scheduler = new TestScheduler(_interpreter, _resultCollector);
            _reportGenerator = new ReportGenerator();
        }

        public void RunTests(string jsonFilePath, bool parallel = false, int maxParallelism = 4)
        {
            try
            {
                // 테스트 케이스 로드
                 List<TestCase> testCases = _loader.LoadTestsFromJson(jsonFilePath);

                // 결과 수집기 초기화
                _resultCollector.Clear();

                // 테스트 실행
                if (parallel)
                {
                    _scheduler.RunParallelAsync(testCases, maxParallelism).Wait();
                }
                else
                {
                    _scheduler.RunSequentially(testCases);
                }

                // 결과 보고서 생성
                List<TestResult> results = _resultCollector.GetResults();
                _reportGenerator.GenerateConsoleReport(results);

                // JSON 보고서 생성
                string reportPath = Path.ChangeExtension(jsonFilePath, ".report.json");
                _reportGenerator.GenerateJsonReport(results, reportPath);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"테스트 실행 중 오류 발생: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
            }
        }
    }

    #endregion

    #region 데이터 모델

    /// <summary>
    /// 테스트 스위트 모델 (JSON 파일 구조)
    /// </summary>
    public class TestSuite
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public List<TestCase> Tests { get; set; }
    }

    /// <summary>
    /// 테스트 케이스 모델
    /// </summary>
    public class TestCase
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Type { get; set; }
        public bool EnableRetry { get; set; }
        public Dictionary<string, object> Parameters { get; set; }

          ////public TestParameters Parameters { get; set; }
    }
    public class TestParameters
    {
        public double TargetVoltage { get; set; }
        public int DurationMs { get; set; }
        public double Tolerance { get; set; } = 0.1;
        public int MaxRetries { get; set; } = 3;
    }

    /// <summary>
    /// 테스트 결과 모델
    /// </summary>
    public class TestResult
    {
        public string TestId { get; set; }
        public string TestName { get; set; }
        public bool Success { get; set; }
        public string ErrorMessage { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public TimeSpan Duration { get; set; }
        public List<TestStep> Steps { get; set; }

        public TestResult(string id, string name)
        {
            TestId = id;
            TestName = name;
            Success = false;
            Steps = new List<TestStep>();
        }
    }

    /// <summary>
    /// 테스트 단계 모델
    /// </summary>
    public class TestStep
    {
        public string Name { get; set; }
        public bool Success { get; set; }
        public string Message { get; set; }

        public TestStep(string name, bool success, string message)
        {
            Name = name;
            Success = success;
            Message = message;
        }
    }

    #endregion

    #region 프로그램 실행 예제

    /// <summary>
    /// 메인 프로그램
    /// </summary>
    public class Program
    {
        public static void Main(string[] args)
        {
            // 샘플 JSON 테스트 파일 생성
            CreateSampleTestFile("bms_tests.json");
            Thread.Sleep(2000);


            // 테스트 실행 엔진 생성 및 실행
            TestExecutionEngine engine = new TestExecutionEngine();

            Console.WriteLine("순차 테스트 실행 중...");
            engine.RunTests("bms_tests.json", false);

            Thread.Sleep(2000);


            Console.WriteLine("\n병렬 테스트 실행 중...");
            engine.RunTests("bms_tests.json", true, 2);

            Thread.Sleep(2000);



            Console.WriteLine("\n모든 테스트 완료. 아무 키나 누르면 종료됩니다.");
            Console.ReadLine();
        }

        /// <summary>
        /// 샘플 테스트 JSON 파일 생성
        /// </summary>
        private static void CreateSampleTestFile(string filePath)
        {
            Console.WriteLine($"샘플 테스트 파일 생성 중: {filePath}");

            var testSuite = new TestSuite
            {
                Name = "BMS 기능 테스트 스위트",
                Description = "BMS 시스템의 전압, 전류, 통신 기능 검증을 위한 테스트 모음",
                Tests = new List<TestCase>
        {
            new TestCase
            {
                Id = "V001",
                Name = "기본 전압 출력 테스트",
                Type = "Voltage",
                EnableRetry = true,
                Parameters = new Dictionary<string, object>
                {
                    { "targetVoltage", 12.0 },
                    { "durationMs", 500 },
                    { "tolerance", 0.2 },
                    { "maxRetries", 2 }
                }
            },
            new TestCase
            {
                Id = "V002",
                Name = "저전압 출력 테스트",
                Type = "Voltage",
                EnableRetry = false,
                Parameters = new Dictionary<string, object>
                {
                    { "targetVoltage", 3.3 },
                    { "durationMs", 300 },
                    { "tolerance", 0.1 }
                }
            },
            new TestCase
            {
                Id = "C001",
                Name = "기본 전류 출력 테스트",
                Type = "Current",
                EnableRetry = true,
                Parameters = new Dictionary<string, object>
                {
                    { "targetCurrent", 5.0 },
                    { "durationMs", 500 },
                    { "tolerance", 0.1 },
                    { "maxRetries", 3 }
                }
            },
            new TestCase
            {
                Id = "CAN001",
                Name = "BMS 상태 요청 테스트",
                Type = "CANComm",
                EnableRetry = true,
                Parameters = new Dictionary<string, object>
                {
                    { "canID", "0x18FF50E5" },
                    { "messageData", "03 22 F0 05 00 00 00 00" },
                    { "timeoutMs", 200 },
                    { "maxRetries", 2 }
                }
            },
            new TestCase
            {
                Id = "CAN002",
                Name = "배터리 SOC 요청 테스트",
                Type = "CANComm",
                EnableRetry = true,
                Parameters = new Dictionary<string, object>
                {
                    { "canID", "0x18FF50E5" },
                    { "messageData", "03 22 F1 89 00 00 00 00" },
                    { "timeoutMs", 200 },
                    { "maxRetries", 2 }
                }
            }
        }
            };

            string jsonContent = JsonSerializer.Serialize(testSuite, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            File.WriteAllText(filePath, jsonContent);
            Console.WriteLine($"샘플 테스트 파일 생성 완료: {filePath}");
        }


    }
    #endregion


    // 파일 상단에 추가할 확장 메서드
    public static class JsonExtensions
    {
        public static T GetValue<T>(this Dictionary<string, object> dict, string key, T defaultValue = default)
        {
            if (!dict.ContainsKey(key))
                return defaultValue;

            var value = dict[key];

            if (value is JsonElement jsonElement)
            {
                // JsonElement를 적절한 타입으로 변환
                if (typeof(T) == typeof(int))
                    return (T)(object)jsonElement.GetInt32();
                else if (typeof(T) == typeof(double))
                    return (T)(object)jsonElement.GetDouble();
                else if (typeof(T) == typeof(string))
                    return (T)(object)jsonElement.GetString();
                else if (typeof(T) == typeof(bool))
                    return (T)(object)jsonElement.GetBoolean();
                // 필요에 따라 다른 타입 추가
            }

            // 일반적인 변환 시도
            return (T)Convert.ChangeType(value, typeof(T));
        }
    }
}