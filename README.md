# 二次元观测器

## 介绍

在人们看“二次元动画”的时候，经常性地需要从一些网站上下载种子，然后手动下载。人们喜欢在手机或者平板电脑上进行“二次元观测”，然而人们往往不喜欢（或者不能）在移动设备上下载种子，因此需要将下载好的“二次元动画”从电脑传输到移动设备上进行观看。这造成了很多不便，因此，二次元观测器横空出世，能够自动化（TODO）或半自动化地从云端下载并且方便您在任何喜爱的设备上观看您喜欢的“二次元动画”。

## 部署说明

### 组件需求

在0.1版本发布之前的任何的构建，需要您下载并安装以下的组件：

[PostgreSQL](https://www.postgresql.org/)

[Asp .Net Core Runtime](https://dotnet.microsoft.com/download/dotnet/5.0/runtime) （如果你用的是Windows，就下载右边的）

[qBittorrent](https://www.qbittorrent.org/)或[qBittorrent Enhanced](https://github.com/qbittorrent/qBittorrent)

之后的版本**可能**会发布自带上述组件的开箱即用版本或者`docker`版本。

### qBittorrent 额外设置

在`设置-Web UI`中，请勾选`Web 用户界面（远程控制）`和`对本地主机上的客户端跳过身份验证`。

### 配置程序

在启动前，您需要手动填写一些配置。

用您喜欢的编辑器打开`appsetting.json`，下面的字段需要您手动填写：

- `FetchUrls`：数组，您的[蜜柑计划](https://mikanani.me/)订阅RSS链接。
- `ConnectionStrings:DefaultConnection`：字符串，你的PostgreSQL链接字符串，如果你不清楚，但是记得安装过程中设置了一个密码，那大概率是`User ID=postgres;Password=你的密码;Host=localhost;Port=5432;Database=sdw;Pooling=true;Connection Lifetime=0;`
- `DownloadDir`：字符串，下载的“二次元动画”储存位置，当然你可以使用默认的。

### 启动程序

若您使用的是Windows，启动`SecondDimensionWatcher.exe`即可，若使用Linux或macOS，启动`SecondDimensionWatcher`或者`dotnet SecondDimensionWatcher.dll`。

默认在[::1]监听5000与5001端口，若要改变请在`appsettings.json`中修改。

5001端口监听的证书默认为自签证书，若要信任自签证书，请运行`dotnet dev-certs https --trust`。若要提供SSL证书，在`appsetting.json`中的`Kestrel:Endpoints:Https:Certificate:Path`中添您`pfx`格式证书的路径，在`Kestrel:Endpoints:Https:Certificate:Password`中添加您`pfx`格式证书的密码。

若您不想使用`https`，可以去掉`Kestrel:EndPoints`中的HTTPS选项。但是为了数据安全，我们强烈建议您使用HTTPS。

此外，也可以通过Nginx反代添加SSL证书。
