using System.Diagnostics;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace BBLegacyServer {
	public static class ClockTimer {
		public static List<int> ProcCaps;
		public static Stopwatch RunTime = new Stopwatch();

		public static void RecordRunTime() {
			RunTime.Start();
		}

		public static long GetRunTime() {
			return RunTime.ElapsedMilliseconds;
		}

		public static void StopRunTime() {
			RunTime.Stop();
		}
	}
}
