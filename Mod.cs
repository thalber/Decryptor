using System;
using Mono.Cecil;
using Mono.Cecil.Cil;
using MonoMod.Cil;

using IO = System.IO;
using UTL = RWCustom.Custom;
namespace Decryptor;

[BepInEx.BepInPlugin("thalber.decryptor", "Decryptor", "0.1")]
public class Mod : BepInEx.BaseUnityPlugin
{
	public void OnEnable()
	{

		Logger.LogError($"DECRYPT ON");

		string ilc = "";
		IL.InGameTranslator.EncryptDecryptFile += (context) =>
		{
			Logger.LogWarning("ilhook go");
			ILCursor c = new(context);
			try
			{
				c.GotoNext(MoveType.After,
					x => x.MatchCall(typeof(IO.Path).FullName, nameof(IO.Path.GetFileNameWithoutExtension))//,
					//x => x.MatchStloc(4)
				);
			}
			catch (Exception ex)
			{
				Logger.LogError(ex);
				return;
			}
			c.Index += 2;
			Logger.LogWarning("found injection point");
			c.Emit(OpCodes.Ldloc_S, context.Body.Variables[4]);
			c.EmitDelegate((string fname) =>
			{
				Logger.LogDebug($"conv {fname}");
				if (fname.IndexOf('-') > -1) return fname[..fname.IndexOf('-')];
				return fname;
			});
			c.Emit(OpCodes.Stloc_S, context.Body.Variables[4]);
			//
			ilc = context.ToString();
		};
		On.RainWorld.OnModsInit += (orig, self) =>
		{
			orig(self);
			IO.File.WriteAllText(IO.Path.Combine(UTL.RootFolderDirectory(), "decildump"), ilc);
			string outroot = IO.Path.Combine(UTL.RootFolderDirectory(), "decrypt");
			Logger.LogWarning(IO.Directory.CreateDirectory(outroot));
			foreach (string rawlang in InGameTranslator.LanguageID.values.entries)
			{
				Logger.LogWarning($"Decrypting language {rawlang}");
				string subdir = IO.Path.Combine(outroot, rawlang[..3]);
				IO.Directory.CreateDirectory(subdir);
				InGameTranslator.LanguageID lang = new(rawlang, false);
				string[] files = AssetManager.ListDirectory($"text/text_{rawlang[..3].ToLower()}");
				Logger.LogWarning(files.Length);
				foreach (string file in files)
				{
					try
					{
						IO.File.WriteAllText(IO.Path.Combine(subdir, IO.Path.GetFileName(file)), InGameTranslator.EncryptDecryptFile(file, false, true));
					}
					catch (Exception ex) { Logger.LogError($"Error decrypting file {ex}"); }
				}
			}
		};

	}

}
