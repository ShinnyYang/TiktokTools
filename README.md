# TiktokTools
TikTok\抖音\快手\本地  视频去重工具， 包含WEB版和window桌面版

去重包括：
    1. 原视频每十分之一的段落会被随机调速
    2. 每十分之一的段落会被删除1帧
    3. 视频轻度锐化
    3. 增加滤镜 (亮度、伽马、对比度等)
    4. 支持视频镜像
    5. 支持 放大/横屏转竖屏(模糊背景)/竖屏转横屏(模糊背景) 三种模式

Program.cs 里可开启自动更新 和 授权码验证 （不用就不用管）

软件版包含远程和本地两种模式读取视频

远程版：
    1.  支持单视频读取 (快手接口不稳定，可自行更换)
    2.  支持批量读取用户的视频列表 (快手请安装Playwright)
本地版：
    1.  单文件
    2.  文件夹
处理后的文件夹可在软件内打开


网页版的基础设置都设置好了，也是有文件版和链接直转的版本（推荐本地版，链接直转依赖的无水印解析方法可能会过期）

文件版：Domain/Home/Index
链接版：Domain/Home/Link

处理后的文件在页面上点击下载