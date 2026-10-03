using CK.Core;
using NUnit.Framework;
using NUnit.Framework.Interfaces;
using NUnit.Framework.Internal;
using System;
using System.Collections.Generic;
using static CK.Testing.MonitorTestHelper;


namespace CK.Testing.NUnit;

/// <summary>
/// Makes each NUnit tests log as groups and logs the <see cref="TestResult.Message"/> and <see cref="TestResult.StackTrace"/>
/// on error into the <see cref="IMonitorTestHelper.Monitor"/>.
/// </summary>
[AttributeUsage( AttributeTargets.Assembly, AllowMultiple = false )]
public class TestHelperMonitorSupportAttribute : Attribute, ITestAction
{
    readonly Stack<IDisposableGroup> _groups;

    /// <summary>
    /// Initializes a new <see cref="TestHelperMonitorSupportAttribute"/>.
    /// </summary>
    public TestHelperMonitorSupportAttribute()
    {
        _groups = new Stack<IDisposableGroup>();
    }

    void ITestAction.BeforeTest( ITest test )
    {
        _groups.Push( TestHelper.Monitor.UnfilteredOpenGroup( LogLevel.Info | LogLevel.IsFiltered, null, $"Running '{test.Name}'.", null ) );
    }

    void ITestAction.AfterTest( ITest test )
    {
        var result = TestExecutionContext.CurrentContext.CurrentResult;
        if( _groups.TryPop( out var g ) )
        {
            if( result.ResultState.Status != TestStatus.Passed )
            {
                g.ConcludeWith( () => result.ResultState.Status.ToString() );
                var message = result.Message;
                if( !string.IsNullOrWhiteSpace( message ) )
                {
                    TestHelper.Monitor.OpenError( message );
                    if( result.StackTrace != null ) TestHelper.Monitor.Trace( result.StackTrace );
                }
            }
            g.Dispose();
        }
        else
        {
            TestHelper.Monitor.UnfilteredLog( LogLevel.Error | LogLevel.IsFiltered, null, $"AftreTest '{test.Name}': test stack is empty, BeforeTest call error.", null );
        }
    }

    ActionTargets ITestAction.Targets => ActionTargets.Test | ActionTargets.Suite;

}
