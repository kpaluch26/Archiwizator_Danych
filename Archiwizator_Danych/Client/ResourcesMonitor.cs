using System;
using System.Diagnostics;
using System.Threading;
using System.Windows;

namespace Client
{
    class ResourcesMonitor
    {
        public static void ResourcesMonitorWork(object _canceltoken, double _totalram, MainWindow _mw)
        {
            CancellationToken canceltoken = (CancellationToken)_canceltoken;
            MainWindow MW = _mw;

            PerformanceCounter cpu_usage = new PerformanceCounter("Processor", "% Processor Time", "_Total");
            PerformanceCounter ram_usage = new PerformanceCounter("Memory", "Available MBytes");
            PerformanceCounter disk_usage = new PerformanceCounter("PhysicalDisk", "% Disk Time", "_Total");
            var firstCall = cpu_usage.NextValue();
            Thread.Sleep(100);

            while (!canceltoken.IsCancellationRequested)
            {
                double cpu = Math.Round(cpu_usage.NextValue(), 2);
                double ram = Math.Round(((_totalram - ram_usage.NextValue()) * 100 / _totalram), 2);
                double disk = Math.Round(disk_usage.NextValue(), 2);
                MW.Dispatcher.Invoke(delegate { ResourcesMonitorUpdate(cpu, ram, disk, MW); });
                Thread.Sleep(1000);
            }
            try
            {
                MW.Dispatcher.Invoke(delegate { ResourcesMonitorUpdate(0, 0, 0, MW); });
            }
            catch
            {

            }
        }

        private static void ResourcesMonitorUpdate(double cpu, double ram, double disk, MainWindow _mw) //funkcja do aktualizacji użycia podzespołów
        {
            MainWindow MW = _mw;
            double safe_usage = 75;

            MW.tbl_ResourcesMonitorAllert.Text = "UWAGA! Duże wykorzystanie podzespołów: ";
            if (cpu > safe_usage)
            {
                MW.tbl_ResourcesMonitorAllert.Text += "CPU ";
            }
            if (ram > safe_usage)
            {
                MW.tbl_ResourcesMonitorAllert.Text += "RAM ";
            }
            if (disk > safe_usage)
            {
                MW.tbl_ResourcesMonitorAllert.Text += "DISK ";
            }
            if (cpu > safe_usage || ram > safe_usage || disk > safe_usage)
            {
                MW.tbl_ResourcesMonitorAllert.Visibility = Visibility.Visible;
            }
            else
            {
                MW.tbl_ResourcesMonitorAllert.Visibility = Visibility.Hidden;
            }

            MW.rpb_CPU.Value = cpu;
            MW.rpb_RAM.Value = ram;
            MW.rpb_DISK.Value = disk;
        }
    }
}
