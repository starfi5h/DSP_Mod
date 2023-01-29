using HarmonyLib;
using System;

namespace AlterTickrate.Patches
{
    public class CargoPath_Patch
    {



		[HarmonyPrefix]
		[HarmonyPatch(typeof(CargoPath), nameof(CargoPath.Update))]
		static bool Update(CargoPath __instance)
		{
#if DEBUG
			if (!ConfigSettings.EnableBelt)
				return true;
#endif

			byte[] obj;
			if (__instance.outputPath != null)
			{
				byte[] array = (__instance.id > __instance.outputPath.id) ? __instance.buffer : __instance.outputPath.buffer;
				obj = ((__instance.id < __instance.outputPath.id) ? __instance.buffer : __instance.outputPath.buffer);
				//lock (obj)
				{
					byte[] obj2 = array;
					//lock (obj2)
					{
						int num = __instance.bufferLength - 5 - 1;
						if (__instance.buffer[num] == 250)
						{
							int cargoId = (int)(__instance.buffer[num + 1] - 1 + (__instance.buffer[num + 2] - 1) * 100) + (int)(__instance.buffer[num + 3] - 1) * 10000 + (int)(__instance.buffer[num + 4] - 1) * 1000000;
							if (__instance.closed)
							{
								// outputPath = this, belt is a circle
								if (__instance.outputPath.TryInsertCargoNoSqueeze(__instance.outputIndex, cargoId))
								{
									Array.Clear(__instance.buffer, num - 4, 10);
									__instance.updateLen = __instance.bufferLength;
								}
							}
							else
							{
								// check next 10 buffers
								if (__instance.outputPath.TryInsertCargo(__instance.outputIndex, cargoId))
								{
									Array.Clear(__instance.buffer, num - 4, 10);
									__instance.updateLen = __instance.bufferLength;
								}
								else if (__instance.outputPath.TryInsertCargo(__instance.outputIndex - 5 * ConfigSettings.BeltUpdatePeriod, cargoId))
                                {
									//Log.Debug(__instance.id);
									Array.Clear(__instance.buffer, num - 4, 10);
									__instance.updateLen = __instance.bufferLength;
								}
							}
						}
						goto IL_167;
					}
				}
			}
			if (__instance.bufferLength <= 10)
			{
				return false;
			}
		IL_167:
			obj = __instance.buffer;
			//lock (obj)
			{
				int num2 = __instance.updateLen - 1;
				while (num2 >= 0 && __instance.buffer[num2] != 0)
				{
					__instance.updateLen--;
					num2--;
				}
				if (__instance.updateLen != 0)
				{
					int num3 = __instance.updateLen;
					for (int i = __instance.chunkCount - 1; i >= 0; i--)
					{
						int num4 = __instance.chunks[i * 3];
						int num5 = __instance.chunks[i * 3 + 2] * ConfigSettings.BeltUpdatePeriod; //ConfigSettings.BeltUpdatePeriod
						if (num4 < num3)
						{
							//if (num4 >= __instance.buffer.Length)
							//	Log.Warn("Out of bound 0");
							if (__instance.buffer[num4] != 0)
							{
								int j = num4 - 5;
								while (j < num4 + 4)
								{
									//if (j >= __instance.buffer.Length)
									//	Log.Warn("Out of bound 1");
									if (j >= 0 && __instance.buffer[j] == 250)
									{
										if (j < num4)
										{
											num4 = j + 5 + 1;
											break;
										}
										num4 = j - 4;
										break;
									}
									else
									{
										j++;
									}
								}
							}
							int k = 0;
							while (k < num5)
							{
								int num6 = num3 - num4;
								if (num6 < 10 * ConfigSettings.BeltUpdatePeriod)
								{
									break;
								}
								int num7 = 0;
								for (int l = 0; l < num5 - k; l++)
								{
									int num8 = num3 - 1 - l;
									//if (num8 >= __instance.buffer.Length || num8 < 0)
									//	Log.Warn("Out of bound 2");
									if (__instance.buffer[num8] != 0)
									{
										break;
									}
									num7++;
								}
								if (num7 > 0)
								{
									try
									{
										Array.Copy(__instance.buffer, num4, __instance.buffer, num4 + num7, num6 - num7);
										Array.Clear(__instance.buffer, num4, num7);
									}
									catch (Exception e)
                                    {
										Log.Warn(e);
                                    }
									k += num7;
								}
								int num9 = num3 - 1;
								while (num9 >= 0 && __instance.buffer[num9] != 0)
								{
									num3--;
									num9--;
								}
							}
							int num10 = num4 + ((k == 0) ? 1 : k);
							if (num3 > num10)
							{
								num3 = num10;
							}
						}
					}
				}
			}
			return false;
		}
	}
}
