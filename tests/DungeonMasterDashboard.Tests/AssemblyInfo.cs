// A fresh test class instance per test, so state such as bUnit services and fake APIs
// never leaks between tests (NUnit otherwise shares one instance per fixture).
[assembly: FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
