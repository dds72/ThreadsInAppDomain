using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Diagnostics.Runtime;

namespace ClrMD
{
	class Program
	{
		static void Main(string[] args)
		{
			//using (var dataTarget = DataTarget.LoadCrashDump(@""))
			using (var dataTarget = DataTarget.AttachToProcess(int.Parse(args[0]), 1000, AttachFlag.NonInvasive))
			{
				var clrVersion = dataTarget.ClrVersions.FirstOrDefault();
				var dacInfo = clrVersion.DacInfo;
				string dacLocation = dataTarget.ClrVersions[0].LocalMatchingDac;
				var runtime = clrVersion.CreateRuntime();

				var appDomains = new Dictionary<ulong, ClrAppDomain>();
				foreach (var appDomain in runtime.AppDomains)
				{
					appDomains[appDomain.Address] = appDomain;
					Console.WriteLine($"AppDomain name\t{appDomain.Name}\t{appDomain.Address}");
				}
				Console.WriteLine();

				var threadNames = new Dictionary<int, string>();
				foreach (var obj in runtime.Heap.EnumerateObjects().Where(o => o.Type.Name == "System.Threading.Thread"))
				{
					var managedThreadId = obj.GetField<int>("m_ManagedThreadId");
					var threadName = obj.GetStringField("m_Name");
					if (!threadNames.ContainsKey(managedThreadId))
						threadNames[managedThreadId] = threadName;

				}

				foreach (var thread in runtime.Threads.OrderBy(t => t.ManagedThreadId))
				{
					if (!threadNames.ContainsKey(thread.ManagedThreadId))
					{
						threadNames[thread.ManagedThreadId] = string.Empty;
					}

					Console.WriteLine(
						$"Thread id\t{thread.ManagedThreadId}\tAppDomain\t{appDomains[thread.AppDomain]}\tname: {threadNames[thread.ManagedThreadId]}\tException: {thread.CurrentException?.Message}");
				}
				Console.WriteLine();

				foreach (var nativeWorkItem in runtime.ThreadPool.EnumerateNativeWorkItems())
				{
					Console.WriteLine($"Native work item type: {nativeWorkItem.Kind}");
				}

			}
		}
	}
}
