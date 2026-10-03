using NUnit.Framework;
using Shouldly;
using System;
using CK.Monitoring;
using System.Diagnostics;

namespace CK.Testing.Tests;

[TestFixture]
public class DebugAssertTests
{
    [Test]
    public void Debug_Fail_does_not_crash_the_test_runner_process_but_throws_an_Exception_with_a_Message_that_starts_with_Assertion_Failed()
    {
        // Even with a Module initializer (see the ModuleInit.Fody package for instance), when this
        // test is run with absolutely no interaction with the CK.Testing objects... The initializer is
        // not yet executed because the assembly itself is not yet loaded/initialized...
        // The module initializer is not a solution: we need to trigger "something"!
        // (And this is why we don't use ModuleInit.Fody...)
        //
        // In "normal" use a TestHelper has been required, this initialization has already been done.
        //
        StaticTestHelper.EnsureInitialized();
        Assume.That( StaticTestHelper._buildConfiguration == "Debug" );
        // Before the TestHelper exists, the SafeTraceListener handles the failure. Once it exists, the MonitorTraceListener
        // of the GrandOutput replaces it: this depends on the tests that already ran in this process.
        bool safeListener = Trace.Listeners["CK.Testing.SafeTraceListener"] != null;
        try
        {
            Debug.Assert( 1 == 0, "This should lead to a simple exception, not the death of the process..." );
            Assert.Fail( "This is NEVER reached!" );
        }
        catch( Exception ex )
        {
            // Everything is fine!
            ex.ShouldNotBeNull();
            if( safeListener )
            {
                ex.Message.ShouldStartWith( "Assertion Failed: " );
            }
            else
            {
                ex.ShouldBeOfType<MonitoringFailFastException>();
            }
        }
    }
}
