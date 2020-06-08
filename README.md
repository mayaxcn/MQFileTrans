# IBMMQ 6.0以上版本测试通过，请使用服务器通道

### App.config配置文件中对相关通道和收发目录进行配置
* HostName MQ服务器地址
* HostPort MQ服务器端口
* SvrCHL 服务器通道名称
* CCSID 字符编码（默认为1381）
* MQName 队列管理器名称
* RecvQueue 提取队列
* RecvFolder 接收文件存放目录
* SendQueue 发送队列
* SendFolder 发送文件存放目录

### 配置示例
```xml
<!--IBMMQ配置信息-->
<add key="HostName" value="localhost"/>
<add key="HostPort" value="2020"/>
<add key="SvrCHL" value="CHL.svr"/>
<add key="CCSID" value="1381"/>
<add key="MQName" value="MQtrans"/>

<!--发送配置信息-->
<add key="RecvQueue" value="RecvQ"/>
<add key="RecvFolder" value="D:\\CustomsPortExchangeFile\\customsTOport\\RECV"/>

<!--接收配置信息-->
<add key="SendQueue" value="SendQ"/>
<add key="SendFolder" value="D:\\CustomsPortExchangeFile\\customsTOport"/>
```
