using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using System.Diagnostics;
using Inspector_SimpleArchitecture;

namespace SimplifiedBMSTestFramework
{  
    #region 테스트 프레임워크 핵심 컴포넌트

    /// <summary>
    /// 테스트 팩토리 - 적절한 테스트 케이스 객체 생성
    /// </summary>
    //public static class TestFactory
    //{
    //    public static TestCase CreateTest(TestConfig testConfig)
    //    {
    //        Console.WriteLine($"테스트 생성 중: {testConfig.Name} (타입: {testConfig.Type})");

    //        switch (testConfig.Type.ToLower())
    //        {
    //            case "voltage":
    //                return new VoltageTestCase(testConfig.Id, testConfig.Name, testConfig.Parameters);

    //            case "current":
    //                return new CurrentTestCase(testConfig.Id, testConfig.Name, testConfig.Parameters);

    //            case "cancomm":
    //                return new CANCommTestCase(testConfig.Id, testConfig.Name, testConfig.Parameters);

    //            default:
    //                throw new ArgumentException($"지원되지 않는 테스트 타입: {testConfig.Type}");
    //        }
    //    }
    //}

    /// <summary>
    /// 테스트 관리자 - 테스트 실행 및 결과 수집 관리
    /// </summary>
    public class TestManager
    {
        private readonly List<TestResult> _results = new List<TestResult>();

        // 테스트 스위트 로드
        public TestSuite LoadTestSuite(string jsonFilePath)
        {
            Console.WriteLine($"테스트 스위트 로드 중: {jsonFilePath}");

            string jsonContent = File.ReadAllText(jsonFilePath);
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
            return JsonSerializer.Deserialize<TestSuite>(jsonContent, options);
        }

        // 순차 테스트 실행
        public void RunSequentially(TestSuite testSuite)
        {
            Console.WriteLine($"===== 순차 테스트 실행 시작 ({testSuite.Tests.Count}개 테스트) =====");
            _results.Clear();

            foreach (var testConfig in testSuite.Tests)
            {
                TestCase test = TestFactory.CreateTest(testConfig);
                test.EnableRetry = testConfig.EnableRetry;
                TestResult result = test.Execute();
                _results.Add(result);
            }

            Console.WriteLine("===== 순차 테스트 실행 완료 =====");
        }

        // 병렬 테스트 실행
        public async Task RunParallelAsync(TestSuite testSuite, int maxParallelism = 4)
        {
            Console.WriteLine($"===== 병렬 테스트 실행 시작 ({testSuite.Tests.Count}개 테스트, 최대 병렬 수: {maxParallelism}) =====");
            _results.Clear();

            // 병렬 처리를 위한 작업 리스트 생성
            var tasks = testSuite.Tests.Select(async testConfig =>
            {
                TestCase test = TestFactory.CreateTest(testConfig);
                test.EnableRetry = testConfig.EnableRetry;
                TestResult result = await Task.Run(() => test.Execute());

                lock (_results)
                {
                    _results.Add(result);
                }
            }).ToList();

            // 병렬 처리 시작 (청크로 나누어 최대 병렬 수 제한)
            var chunkedTasks = tasks.Chunk(maxParallelism).ToList();
            foreach (var chunk in chunkedTasks)
            {
                await Task.WhenAll(chunk);
            }

            Console.WriteLine("===== 병렬 테스트 실행 완료 =====");
        }

        // 결과 가져오기
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

    #region 데이터 모델
    /// <summary>
    /// 테스트 스위트 모델 (JSON 파일 구조)
    /// </summary>
    public class TestSuite
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public List<TestConfig> Tests { get; set; }
    }
    /// <summary>
    /// 테스트 구성 모델
    /// </summary>
    public class TestConfig
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Type { get; set; }
        public bool EnableRetry { get; set; }
        public Dictionary<string, object> Parameters { get; set; }
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
            Thread.Sleep(1000);

            // 테스트 관리자 생성
            var testManager = new TestManager();

            // 테스트 스위트 로드
            var testSuite = testManager.LoadTestSuite("bms_tests.json");

            // 순차 테스트 실행
            Console.WriteLine("\n순차 테스트 실행 중...");
            testManager.RunSequentially(testSuite);
            testManager.GenerateConsoleReport();
            testManager.GenerateJsonReport("sequential_report.json");

            Thread.Sleep(1000);

            // 병렬 테스트 실행
            Console.WriteLine("\n병렬 테스트 실행 중...");
            testManager.RunParallelAsync(testSuite, 2).Wait();
            testManager.GenerateConsoleReport();
            testManager.GenerateJsonReport("parallel_report.json");

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
                Tests = new List<TestConfig>
                {
                    new TestConfig
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
                    new TestConfig
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
                    new TestConfig
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
                    new TestConfig
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
                    new TestConfig
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
}