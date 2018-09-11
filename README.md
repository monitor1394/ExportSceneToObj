
# ExportSceneToObj
`ExportSceneToObj`是用于导出`Unity`场景（包括`GameObject`和`Terrian`）到`.obj`文件的`Editor`脚本，也支持导出`.fbx`模型到`.obj`。

### 功能：
* 支持导出物件和地形
* 支持自定义裁剪区域
* 支持自动裁剪功能
* 支持单个选择导出
* 支持导出`.fbx`模型

### 用法：
* 将`ExportScene.cs`脚本放到你项目的`Assets`/`Editor`目录下
* 如果要自定义裁剪区域的话，场景中增加空`GameObject`用于表示裁剪区域（需要左下角和右上角两个空`GameObject`），并修改代码中`CUT_LB_OBJ_PATH`和`CUT_RT_OBJ_PATH`为对应的路径
* 在`Unity`的菜单栏上有`ExportScene`菜单即可
* 怎么单独导出`.fbx`模型？
    1. 将`.fbx`拖到场景中
    2. 在`Hierarchy`试图中选中`fbx`的`GameObject`，右键执行`ExportScene` --> `ExportSelectedObj`单独导出即可

### 其他：
1. 目前判断物件是否在裁剪区域只是判断物件的坐标是否在区域内，还没有实现物件边界裁剪。
2. 只有包含`MeshFilter`、`SkinnedMeshRenderer`、`Terrian`的物件才会被导出

### 问题：
1. 为什么将脚本放入项目中后菜单栏还是看不到`ExportScene`菜单项？  
   答：脚本文件放到正确的目录，同时要检查是否有其他脚本有报错没有编译通过，有报错时先要处理报错。  
2. 为什么导出的`obj`文件在`Maya`等`3D`软件中显示正常,但在`3d Max`显示异常？  
   答：`3d Max`导入设置中勾选`Import as single mesh`选项。  
   
### 觉得有用的朋友帮忙点个star吧

### 参考：
1. [ExportOBJ](http://wiki.unity3d.com/index.php?title=ExportOBJ)
2. [TerrainObjExporter](http://wiki.unity3d.com/index.php?title=TerrainObjExporter)
