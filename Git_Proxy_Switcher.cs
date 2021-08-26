using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Git_Proxy_Switcher
{
    public static class Proxywatcher
    {
        static RegistryKey OpenKey()
        {
            return Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Internet Settings", true);
        }
        public static int get_system_proxy_status()
        {
            RegistryKey registryKey = OpenKey();
            return Int32.Parse(registryKey.GetValue("ProxyEnable").ToString());
        }
        public static String Get_system_proxy_addr()
        {
            RegistryKey registryKey = OpenKey();
            return registryKey.GetValue("ProxyServer").ToString();
        }
    }
    public static class Processeswatcher
    {
        const string Aim_process = "Clash for Windows";
        public static string Processisrunning()
        {
            if(Aim_process == "")
            {
                return "process_running";//在不使用进程检测的时候直接跳过
            }
            Process[] ps = Process.GetProcesses();
            foreach (Process p in ps)
            {
                string info = "";
                try
                {
                    if (p.ProcessName == Aim_process)
                    {
                        return "process_running";
                    }
                }
                catch (Exception e)
                {
                    info = e.Message;
                    return "info";
                }
            }
            return "process_not_running";
        }
    }
    class Git_Proxy_Switcher
    {
        [DllImport("user32.dll", EntryPoint = "ShowWindow", SetLastError = true)]
        static extern bool ShowWindow(IntPtr hWnd, uint nCmdShow);
        [DllImport("user32.dll", EntryPoint = "FindWindow", SetLastError = true)]
        public static extern IntPtr FindWindow(string lpClassName, string lpWindowName);
        static void Main(string[] args)
        {
            Console.Title = "PROXY_WATCHER";
            IntPtr intptr = FindWindow("ConsoleWindowClass", "PROXY_WATCHER");
            if (intptr != IntPtr.Zero)
            {
            ShowWindow(intptr, 0);//隐藏这个窗口
            }
            bool current_state = false;//建立一个临时变量，用于存储当前状态
            while (true)
            {
                //开始处理代理信息
                Process p = new Process();
                p.StartInfo.FileName = "cmd.exe";//启动cmd
                p.StartInfo.UseShellExecute = false;
                p.StartInfo.RedirectStandardInput = true;
                p.StartInfo.RedirectStandardOutput = true;
                p.StartInfo.RedirectStandardError = true;
                p.StartInfo.CreateNoWindow = true;//不显示窗口

                p.Start();

                string aimproce= Processeswatcher.Processisrunning();
                if (aimproce == "process_running")
                {
                    //Console.WriteLine("process is runing!");
                    if(Proxywatcher.get_system_proxy_status() == 1)
                    {
                        if (!current_state)
                        {
                            Console.WriteLine("proxy set!，addr = {0:C}", Proxywatcher.Get_system_proxy_addr());
                            //输入信息
                            p.StandardInput.WriteLine("git config --global http.proxy " + Proxywatcher.Get_system_proxy_addr());
                            p.StandardInput.AutoFlush = true;
                            p.StandardInput.WriteLine("git config --global httpS.proxy " + Proxywatcher.Get_system_proxy_addr());
                            p.StandardInput.AutoFlush = true;
                            current_state = true;
                        }
                        
                    }
                    else
                    {
                        if (current_state)
                        {
                            Console.WriteLine("proxy unset, remove git proxy info");
                            //输入信息
                            p.StandardInput.WriteLine("git config --global --unset http.proxy");
                            p.StandardInput.AutoFlush = true;
                            p.StandardInput.WriteLine("git config --global --unset https.proxy");
                            p.StandardInput.AutoFlush = true;
                            current_state = false;
                        }
                    }
                }
                else if (aimproce == "process_not_running")
                {
                    if (current_state)
                    {
                        Console.WriteLine("proxy not running, remove git proxy info");
                        //输入信息
                        p.StandardInput.WriteLine("git config --global --unset http.proxy");
                        p.StandardInput.AutoFlush = true;
                        p.StandardInput.WriteLine("git config --global --unset https.proxy");
                        p.StandardInput.AutoFlush = true;
                        current_state = false;
                    }
                }
                else
                {
                    if (current_state)
                    {
                        Console.WriteLine("proxy program err:"+ current_state);
                    }
                }
                //获取输出信息
                //string strOuput = p.StandardOutput.ReadToEnd();
                //等待程序执行完退出进程
                p.StandardInput.WriteLine("exit");//所有指令运行结束，发送退出指令，cmd会停止
                p.WaitForExit();
                p.Close();
                System.Threading.Thread.Sleep(1000);//1秒判断一次
            }
        }
    }
}
