using System;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Threading;

namespace WebGL.Sample
{
	public static unsafe class InputBridge
	{
		static SharedInputMemory* _sharedPtr;
		static InputBuffer* _currentReadBuffer;

		public static Vector2 CanvasSize = Vector2.One;
		public static bool IsFullscreen = false;

		public static IntPtr Initialize()
		{
			// 分配对齐的非托管内存
			void* ptr = NativeMemory.AlignedAlloc((nuint)sizeof(SharedInputMemory), 16);
			NativeMemory.Clear(ptr, (nuint)sizeof(SharedInputMemory));
			_sharedPtr = (SharedInputMemory*)ptr;
			return (IntPtr)ptr;
		}

		public static void BeforeFrame()
		{
			if (_sharedPtr == null)
			{
				return;
			}
			// 1. 原子翻转 Buffer
			int currentIndex = _sharedPtr->ActiveIndex;
			int nextIndex = (currentIndex + 1) % 2;
			Interlocked.Exchange(ref _sharedPtr->ActiveIndex, nextIndex);
			// 2. 获取刚才 JS 写入的 Buffer
			_currentReadBuffer = currentIndex == 0 ? &_sharedPtr->Buffer0 : &_sharedPtr->Buffer1;
			// 3. 处理状态数据
			ProcessStates();
			// 4. 处理事件流
			ProcessEvents();
			// 5. 重置事件计数 (状态数据不需要清零，会覆盖)
			_currentReadBuffer->UsedBytes = 0;
		}

		static void ProcessStates()
		{
			float newCanvasWidth = _currentReadBuffer->CanvasWidth;
			float newCanvasHeight = _currentReadBuffer->CanvasHeight;
			if (CanvasSize.X != newCanvasWidth
				|| CanvasSize.Y != newCanvasHeight)
			{
				CanvasSize = new Vector2(newCanvasWidth, newCanvasHeight);
				Test.CanvasResized((int)newCanvasWidth, (int)newCanvasHeight);
			}
			float mousePositionX = _currentReadBuffer->MousePositionX;
			float mousePositionY = _currentReadBuffer->MousePositionY;
			// Do something with mouse position
			// Do something with gamepad state
		}

		static void ProcessEvents()
		{
			byte* ptr = _currentReadBuffer->EventData;
			byte* end = ptr + _currentReadBuffer->UsedBytes;
			while (ptr < end)
			{
				InputEventType type = (InputEventType)(*ptr);
				if (type == InputEventType.None)
				{
					return;
				}
				if (((byte)type & 128) == 128)
				{
					// 读取 12 字节
					LargeEvent* e = (LargeEvent*)ptr;
					HandleLargeEvent(e);
					ptr += 12;
				}
				else
				{
					// 读取 4 字节
					SmallEvent* e = (SmallEvent*)ptr;
					HandleSmallEvent(e);
					ptr += 4;
				}
			}
		}

		static void HandleSmallEvent(SmallEvent* e)
		{
			switch (e->Type)
			{
				case InputEventType.KeyDown: Console.WriteLine($"KeyDown: {e->Param}, Char: {(char)(e->Payload)}"); break;
				case InputEventType.KeyUp: Console.WriteLine($"KeyUp: {e->Param}"); break;
				case InputEventType.GamepadButtonDown: Console.WriteLine($"GamepadButtonDown: {e->Param}, GamepadIndex: {e->Payload}"); break;
				case InputEventType.GamepadButtonUp: Console.WriteLine($"GamepadButtonUp: {e->Param}, GamepadIndex: {e->Payload}"); break;
				case InputEventType.GamepadDisconnected: Console.WriteLine($"GamepadDisconnected: {e->Payload}"); break;
			}
		}

		static void HandleLargeEvent(LargeEvent* e)
		{
			switch (e->Header.Type)
			{
				case InputEventType.MouseDown: Console.WriteLine($"MouseDown: {e->Header.Param}, X: {e->X}, Y: {e->Y}"); break;
				case InputEventType.MouseUp: Console.WriteLine($"MouseUp: {e->Header.Param}, X: {e->X}, Y: {e->Y}"); break;
				case InputEventType.MouseMove:
					// Do something with mouse move
					break;
				case InputEventType.MouseWheel: Console.WriteLine($"MouseWheel: {e->Y}"); break;
				case InputEventType.TouchDown: Console.WriteLine($"TouchDown: {e->Header.Param}, X: {e->X}, Y: {e->Y}"); break;
				case InputEventType.TouchUp: Console.WriteLine($"TouchUp: {e->Header.Param}, X: {e->X}, Y: {e->Y}"); break;
				case InputEventType.TouchMove:
					// Do something with touch move
					break;
			}
		}
	}
}
