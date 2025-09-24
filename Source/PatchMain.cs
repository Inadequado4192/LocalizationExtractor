using System.Reflection;
using HarmonyLib;
using Verse;

[StaticConstructorOnStartup]
public static class PatchMain
{
	static PatchMain()
	{
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_000c: Expected O, but got Unknown
		Harmony val = new Harmony("Translate_Patch");
		val.PatchAll(Assembly.GetExecutingAssembly());
	}
}
