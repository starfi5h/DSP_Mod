# BepInEx Mods for Dyson Sphere Program

You can download [my mods](https://dsp.thunderstore.io/package/starfi5h/) in thunderstoe website.  
If there are errors, you can create a issue here, or contact me in dyson sphere offical or modding discord server.  
Codes are license under MIT and free to use.  
  
For supportive mods, check the other repo [DSP_Mod_Support](https://github.com/starfi5h/DSP_Mod_Support).  

## Setting up a development environment

1. Fork the repository to your own Github account.
2. Pull git repository locally `git clone ...`
3. Create a folder named `assemblies` in the repo folder
4. Put the following reference dll files into `assemblies` folder  
```
Assembly-CSharp-publicized.dll  
UnityEngine.CoreModule.dll  
UnityEngine.dll  
UnityEngine.IMGUIModule.dll  
UnityEngine.InputLegacyModule.dll
UnityEngine.TextRenderingModule.dll
UnityEngine.UI.dll
UnityEngine.UIModule.dll
```

You can find the Unity modules in game install folder `Dyson Sphere Program\DSPGAME_Data\Managed`  
`Assembly-CSharp-publicized.dll` can be obtained by using [AssemblyPublicizer](https://github.com/BepInEx/BepInEx.AssemblyPublicizer) on game's `Assembly-CSharp.dll` in the same folder.   