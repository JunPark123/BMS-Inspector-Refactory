using SimplifiedBMSTestFramework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Inspector_SimpleArchitecture
{
    public interface ITestCaseFactory
    {
        TestCase Create(string id, string name, Dictionary<string, object> parameters);
    }

    public static class TestFactory
    {
        private static readonly Dictionary<string, ITestCaseFactory> _factories = new Dictionary<string, ITestCaseFactory>();

        static TestFactory()
        {
            // 여기에 새로운 테스트 케이스 유형을 추가하면 됨
            Register("voltage", new VoltageTestFactory());
            Register("current", new CurrentTestFactory());
            Register("cancomm", new CANCommTestFactory());
        }

        public static void Register(string testType, ITestCaseFactory factory)
        {
            _factories[testType.ToLower()] = factory;
        }

        public static TestCase CreateTest(TestConfig testConfig)
        {
            if (_factories.TryGetValue(testConfig.Type.ToLower(), out var factory))
            {
                return factory.Create(testConfig.Id, testConfig.Name, testConfig.Parameters);
            }
            throw new ArgumentException($"지원되지 않는 테스트 타입: {testConfig.Type}");
        }
    }

    public class VoltageTestFactory : ITestCaseFactory
    {
        public TestCase Create(string id, string name, Dictionary<string, object> parameters)
        {
            return new VoltageTestCase(id, name, parameters);
        }
    }

    public class CurrentTestFactory : ITestCaseFactory
    {
        public TestCase Create(string id, string name, Dictionary<string, object> parameters)
        {
            return new CurrentTestCase(id, name, parameters);
        }
    }

    public class CANCommTestFactory : ITestCaseFactory
    {
        public TestCase Create(string id, string name, Dictionary<string, object> parameters)
        {
            return new CANCommTestCase(id, name, parameters);
        }
    }


}
