# NewKinectFittingRoom

## 添加衣服模型方法
1. 对于每个fbx模型，导入模型并在Unity编辑器的`Assets-view`中选择它。
2. 在`Inspector`中选择`Rig-tab`选项卡。将`AnimationType`设置为`Humanoid`，将`AvatarDefinition`设置为`Create from this model`。
3. 按`Apply`按钮。然后按`Configure`按钮检查是否正确分配了所有必需的关节。服装模特通常不使用所有关节，这可能使头像定义无效。在这种情况下，您可以手动分配:bug:缺失的关节`（以红色显示）`。
4. 请记住：模型中的关节位置必须与Kinect关节的结构相匹配。你可以看到它们，例如在KinectOverlayDemo2中。否则，:bug:模型可能无法正确覆盖用户的身体。
5. 在`FittingRoom/Resources`文件夹中为您的模型类别（衬衫，裤子，裙子等）创建一个子文件夹。
6. 在模型类别文件夹中创建后续编号为（0000,0001,0002等）子文件夹。
7. 将模型移动到这些数字文件夹中，每个文件夹一个模型，以及所需的材料和纹理。将模型的fbx文件重命名为`model.fbx`。
8. 您可以在相应的模型文件夹中以jpeg格式（100 x 143px，24bpp）为每个模型放置预览图像。然后将其重命名为`preview.jpg.bytes`。如果您没有放置预览图像，试衣间演示将在模型选择菜单中显示:bug:`无预览`。
9. 打开`KinectFittingRoom`场景。   
10. 将模型类别的`ModelSelector.cs`组件添加到`KinectController`游戏对象。将其`Model category`设置为与上面第5步中创建的子文件夹名称相同。设置`Number of models`设置以反映上面第6步中创建的子文件夹数。
11. `ModelSelector.cs`组件的其他设置必须与演示中的现有`ModelSelector.cs`类似。即`Model relative to camera`必须设置为`BackgroundCamera`，`Foreground camera`必须设置为`MainCamera`，`Continuous scaling`-启用。`scale-factor`设置最初可以设置为1，`Vertical offset`设置为0.稍后您可以稍微调整它们以提供最佳的模型到主体叠加。
12. 如果希望在模型类别更改后所选模型继续覆盖用户的身体，则启用`ModelSelector.cs`组件的`Keep selected model`设置。这很好用，如果有几个类别（即ModelSelectors），例如衬衫，裤子，裙子等。在这种情况下，当类别改变并且用户开始选择裤子时，所选衬衫模型仍将覆盖用户的身体，实例。
13. `CategorySelector.cs`组件为改变模型和类别提供手势控制，并负责为同一用户切换模型类别（例如衬衫，裤子，领带等）。场景中的第一个用户（播放器索引0）已经有一个`CategorySelector.cs`，:bug:因此您无需添加更多内容。
14. 如果您计划多用户试衣间，请为每个其他用户添加一个`CategorySelector.cs`组件。您可能还需要为这些用户将使用的模型类别添加相应的`ModelSelector.cs`组件。
15. 运行场景以确保可以在列表中选择模型并正确覆盖用户的身体。如果需要，可以进行一些实验，以找到提供最佳模型到主体叠加的比例因子和垂直偏移设置的值。
16. 如果要关闭场景中的光标交互，请禁用`KinectController`游戏对象的`InteractionManager.cs`组件。如果要关闭手势（挥手换衣和举手更换衣服类型），请禁用`CategorySelector.cs`组件的相应设置。如果要关闭或更改`T型姿势`校准，请更改`KinectManager.cs`组件的`Player calibration pose`设置。
17. 如果服装/叠加模型使用标准着色器，请:bug:将其`Rendering mode`设置为`Cutout`。

## 启用人体和模型混合(体混合)，或禁用它以增加FPS
* 如果在KinectFittingRoom场景中选择MainCamera，找到名为`UserBodyBlender.cs`的组件。
* `UserBodyBlender.cs`组件负责将服装模型与用户身体混合。
* 您可以启用该组件，以打开用户的身体与模型混合功能。
* `Depth threshold`设置可用于调整到模型前面的最小距离（以米为单位）。它决定了现实世界的物体何时可见。
* 如果场景性能（FPS）不足，并且混合模式并不重要，则可以禁用`UserBodyBlender.cs`组件以提高性能。

## 替换场景彩色摄像机背景
1. 在场景中启用`KinectController`游戏对象的`BackgroundRemovalManager.cs`组件。
2. 确保`KinectController`的组件`KinectManager.cs`的`Compute user map`设置为`Body texture`，并启用`Compute color map`设置。
3. 将所需的背景图像设置为场景中游戏对象`BackgroundImage1`的`RawImage`组件的纹理。
4. 运行场景以检查它是否按预期工作。
