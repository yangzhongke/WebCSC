using Microsoft.JSInterop;
using WebCSC;

namespace System;
public static class Console
{
    public static void WriteLine(object msg)
    {
        WebCSCMain.JSInProcRuntime.InvokeVoid("alert", Convert.ToString(msg));
    }

    public static void WriteLine(string msg)
    {
        WriteLine((object)msg);
    }

    public static void WriteLine(string format, params object?[] arg)
    {
        WriteLine(string.Format(format,arg));
    }

    public static string ReadLine()
    {
        //IJSRuntime必须是异步的，而IJSInProcessRuntime提供了同步调用的方法，
        //所以这里必须用IJSInProcessRuntime，否则会卡死
        return WebCSCMain.JSInProcRuntime.Invoke<string>("prompt");
    }

    public static string Title
    {
        get
        {
            return WebCSCMain.JSInProcRuntime.Invoke<string>("eval", "document.title");
        }
        set
        {
            WebCSCMain.JSInProcRuntime.Invoke<string>("eval", $"document.title='{value}'");
        }
    }

    public static void ReadKey()
    {
        WebCSCMain.JSInProcRuntime.InvokeVoid("alert", "Press space or enter to continue.");
    }
}