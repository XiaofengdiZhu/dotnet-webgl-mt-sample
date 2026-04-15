using System;
using System.Runtime.InteropServices.JavaScript;
using System.Threading.Tasks;

using Silk.NET.OpenGLES;

namespace WebGL.Sample;

internal static partial class Interop
{
	[JSImport("initialize", "main.js")]
	public static partial void Initialize(IntPtr sharedInputMemoryPtr);

	[JSExport]
	public static async Task OnGamepadConnected(int index, string name)
	{
		Console.WriteLine($"Gamepad {index} connected: {name}");
	}
}
