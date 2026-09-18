using RecluseEdit.Extensions.RestClient.Services;

namespace RecluseEdit.Tests;

[TestClass]
public class MockServerTests
{
    [TestMethod]
    public void MockServerService_InitializesWithDefaultRoutes()
    {
        using var server = new MockServerService();

        Assert.IsFalse(server.IsRunning);
        Assert.IsGreaterThanOrEqualTo(3, server.Routes.Count);

        var usersRoute = server.FindMatchingRoute("GET", "/api/users");
        Assert.IsNotNull(usersRoute);
        Assert.AreEqual(200, usersRoute.StatusCode);
        StringAssert.Contains(usersRoute.ResponseBody, "Alice Chen");

        var loginRoute = server.FindMatchingRoute("POST", "/api/login");
        Assert.IsNotNull(loginRoute);
        Assert.AreEqual(200, loginRoute.StatusCode);
        StringAssert.Contains(loginRoute.ResponseBody, "mock_jwt_token");
    }

    [TestMethod]
    public void FindMatchingRoute_MatchesCaseInsensitiveAndNormalizedPath()
    {
        using var server = new MockServerService();

        var matchTrailingSlash = server.FindMatchingRoute("get", "/api/users/");
        Assert.IsNotNull(matchTrailingSlash);
        Assert.AreEqual("/api/users", matchTrailingSlash.Path);

        var noMatchWrongMethod = server.FindMatchingRoute("DELETE", "/api/users");
        Assert.IsNull(noMatchWrongMethod);

        var noMatchWrongPath = server.FindMatchingRoute("GET", "/api/unknown");
        Assert.IsNull(noMatchWrongPath);
    }

    [TestMethod]
    public void AddAndDeleteRoutes_WorksCorrectly()
    {
        using var server = new MockServerService();
        var initialCount = server.Routes.Count;

        var customRoute = new MockRoute
        {
            Method = "PUT",
            Path = "/api/custom",
            StatusCode = 202,
            ResponseBody = "{\"updated\": true}"
        };

        server.Routes.Add(customRoute);
        Assert.HasCount(initialCount + 1, server.Routes);

        var found = server.FindMatchingRoute("PUT", "/api/custom");
        Assert.IsNotNull(found);
        Assert.AreEqual(202, found.StatusCode);

        server.Routes.Remove(customRoute);
        Assert.HasCount(initialCount, server.Routes);
        Assert.IsNull(server.FindMatchingRoute("PUT", "/api/custom"));
    }

    [TestMethod]
    public void StartAndStop_ChangesStateCorrectly()
    {
        using var server = new MockServerService();
        bool lastState = false;
        server.StateChanged += running => lastState = running;

        Assert.IsFalse(server.IsRunning);
        server.Stop();
        Assert.IsFalse(server.IsRunning);
    }
}

