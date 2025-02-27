﻿// Copyright (c) 2022 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.


using System;
using ExamplesCommonCode.IdentityCookieCode;
using Test.TestHelpers;
using TestSupport.EfHelpers;
using TestSupport.Helpers;
using Xunit;
using Xunit.Abstractions;
using Xunit.Extensions.AssertExtensions;

namespace Test.UnitTests.TestCommonCode;

public class TestPoorMansGlobalCache
{
    private readonly ITestOutputHelper _output;

    public TestPoorMansGlobalCache(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void TestSetGlobalChangeTimeToNowUtc_And_GetGlobalChangeTimeUtc_NoFile()
    {
        //SETUP
        var stubEnv = new StubWebHostEnvironment { WebRootPath = TestData.GetTestDataDir() };
        var service = new GlobalFileStoreManager(stubEnv);
        service.Remove("Test");

        //ATTEMPT
        var content = service.Get("Test");

        //VERIFY
        content.ShouldBeNull();
    }

    [Fact]
    public void TestSetGlobalChangeTimeToNowUtc_And_GetGlobalChangeTimeUtc_ExistingFile()
    {
        //SETUP
        var stubEnv = new StubWebHostEnvironment { WebRootPath = TestData.GetTestDataDir() };
        var service = new GlobalFileStoreManager(stubEnv);
        var guid = Guid.NewGuid().ToString();

        //ATTEMPT
        service.Set("Test", guid);

        //VERIFY
        service.Get("Test").ShouldEqual(guid);
    }

    [Fact]
    public void TestGetGlobalChangeTimeUtc_Performance_Exists()
    {
        //SETUP
        var stubEnv = new StubWebHostEnvironment { WebRootPath = TestData.GetTestDataDir() };
        var service = new GlobalFileStoreManager(stubEnv);
        service.Set("Test", "hello");

        //ATTEMPT
        for (int i = 0; i < 10; i++)
        {
            using (new TimeThings(_output, "write"))
                service.Get("PerformanceTest");
        }

        //VERIFY
    }

    [Fact]
    public void TestGetGlobalChangeTimeUtc_Performance_NoExists()
    {
        //SETUP
        var stubEnv = new StubWebHostEnvironment { WebRootPath = TestData.GetTestDataDir() };
        var service = new GlobalFileStoreManager(stubEnv);
        service.Remove("Test");

        //ATTEMPT
        for (int i = 0; i < 10; i++)
        {
            using (new TimeThings(_output, "write"))
                service.Get("PerformanceTest");
        }

        //VERIFY
    }
}