using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Inspector_SimpleArchitecture
{
    #region 핵심 인터페이스와 클래스

    /// <summary>
    /// 테스트 수행 결과
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
        public List<TestStep> Steps { get; set; } = new List<TestStep>();

        public TestResult(string id, string name)
        {
            TestId = id;
            TestName = name;
            Success = false;
        }
    }

    /// <summary>
    /// 테스트 단계 정보
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
      /// <summary>
     /// 모든 테스트 케이스의 기본 클래스
     /// </summary>
    public abstract class TestCase
    {
        public string Id { get; protected set; }
        public string Name { get; protected set; }
        public Dictionary<string, object> Parameters { get; protected set; }
        public bool EnableRetry { get; set; }
        public int MaxRetries { get; set; } = 3;
        public bool EnableTiming { get; set; } = true;

        protected TestCase(string id, string name, Dictionary<string, object> parameters)
        {
            Id = id;
            Name = name;
            Parameters = parameters;
        }

        /// <summary>
        /// 테스트 실행
        /// </summary>
        public TestResult Execute()
        {
            TestResult result = new TestResult(Id, Name);
            result.StartTime = DateTime.Now;
            Stopwatch stopwatch = null;

            // 타이밍 측정 시작 (옵션)
            if (EnableTiming)
            {
                stopwatch = Stopwatch.StartNew();
                Console.WriteLine($"[타이밍] '{Name}' 테스트 시작");
            }

            // 자동 재시도 처리
            int attempts = 0;
            bool testSuccess = false;

            do
            {
                attempts++;
                if (EnableRetry && attempts > 1)
                {
                    Console.WriteLine($"[재시도] '{Name}' 테스트 실행 (시도 {attempts}/{MaxRetries + 1})");
                }

                try
                {
                    Console.WriteLine($"테스트 시작: {Name} (ID: {Id})");

                    // 1단계: 초기화
                    Initialize();
                    result.Steps.Add(new TestStep("초기화", true, "초기화 완료"));

                    // 2단계: 실행
                    RunTest();
                    result.Steps.Add(new TestStep("실행", true, "실행 완료"));

                    // 3단계: 검증
                    bool verificationResult = Verify();
                    result.Steps.Add(new TestStep("검증", verificationResult,
                        verificationResult ? "검증 성공" : "검증 실패"));

                    testSuccess = verificationResult;
                    if (testSuccess) break;
                }
                catch (Exception ex)
                {
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

                // 재시도 여부 결정
                if (!testSuccess && EnableRetry && attempts <= MaxRetries)
                {
                    Console.WriteLine($"[재시도] '{Name}' 테스트 실패, 1초 후 재시도...");
                    Thread.Sleep(1000);
                    result.Steps.Clear(); // 이전 시도 단계 기록 초기화
                }

            } while (!testSuccess && EnableRetry && attempts <= MaxRetries);

            result.EndTime = DateTime.Now;
            result.Duration = result.EndTime - result.StartTime;
            result.Success = testSuccess;

            // 타이밍 측정 종료 (옵션)
            if (EnableTiming && stopwatch != null)
            {
                stopwatch.Stop();
                Console.WriteLine($"[타이밍] '{Name}' 테스트 완료: {stopwatch.ElapsedMilliseconds}ms");
            }

            // 재시도 결과 출력 (옵션)
            if (EnableRetry && attempts > 1)
            {
                if (!result.Success)
                {
                    Console.WriteLine($"[재시도] '{Name}' 테스트 최종 실패 ({attempts} 시도)");
                }
                else
                {
                    Console.WriteLine($"[재시도] '{Name}' 테스트 {attempts}번째 시도에서 성공");
                }
            }

            Console.WriteLine($"테스트 종료: {Name} - {(result.Success ? "성공" : "실패")} ({result.Duration.TotalSeconds}초)");
            return result;
        }

        // 하위 클래스에서 구현할 추상 메소드들
        protected abstract void Initialize();
        protected abstract void RunTest();
        protected abstract bool Verify();
        protected abstract void Cleanup();
    }

    #endregion
}
