# iffnsLocalMovementSystemForVRChat
A system that allows players to walk on and transition between moving objects in VRChat. 
 
Requires VRChat SDK3 for Worlds, the compatible Unity version and UdonSharp 0.20.3 or higher.

The demo world is available here: https://vrchat.com/home/world/wrld_59005cf7-d488-47e2-bb7b-13474a6604f2

## How to use
### How to use guide:
https://docs.google.com/presentation/d/1AEL2s8zkA7NxHWXrC2KvhZrL5BJv1oBwBJ77J8YthIY

### Unity package download:
https://github.com/iffn/iffnsLocalMovementSystemForVRChat/releases

### Integration without Submodules
To maintain compatability with other projects, please put everything into ```/Assets/iffnsStuff/iffnsLocalMovementSystemForVRChat``` 

### Git Submodule integration
Add this submodule with the following git command (Assuming the root of your Git project is the Unity project folder)
```
git submodule add https://github.com/iffn/iffnsLocalMovementSystemForVRChat.git Assets/iffnsStuff/iffnsLocalMovementSystemForVRChat
```

If you have manually added it, use this one first. (I recommend to close the Unity project first)
```
git rm Assets/iffnsLocalMovementSystemForVRChat -r
```
## License
This work is licensed under a [Creative Commons Attribution-NonCommercial 4.0 International License](https://creativecommons.org/licenses/by-nc/4.0/).

![CC BY-NC 4.0](https://i.creativecommons.org/l/by-nc/4.0/88x31.png)
