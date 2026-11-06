# MapleDash Windows 版托管程序集恢复（P1）

> 这是**从用户提供的 Unity Mono Windows 构建包提取的中间语言（IL）**，不是已经恢复出的原始 C# / Unity 工程，也不代表已能编译或启动独立服务端。

## 输入及校验
- 原始目标：`Windows/MapleDash_Data/Managed/Assembly-CSharp.dll`
- DLL SHA-256：`cfaa6d7f088a66259eb6a29c6e55dacfe2c6d3029c21d813daa7951d030c14a1`
- 本归档：`MapleDash-IL-Recovery-P1.zip`
- 归档 SHA-256：`9d333bdf7fd81f81fefd0fcfc1316d4d17b058c075ecdc9d82c64c49624c5de3`
- 归档大小：3,497,657 字节；由于 GitHub 写入接口单对象传输失败，分成 10 个二进制部分保存。

## 恢复成果
- 读取程序集元数据，识别 2,356 个类型定义、14,302 个方法定义
- 成功提取和反汇编 14,180 个带方法体的 IL 方法
- 按类型生成 2,152 份 `.il` 文件，按 `Core/`、`Server/`、`Other/` 归类
- 另附 `MapleDash-classes-and-methods.txt`、`il_extract.py`、`il-recovery/inventory.json`

## 还原归档
在本目录运行（Python 3，不需要第三方依赖）：

```sh
python3 reassemble.py
unzip -l MapleDash-IL-Recovery-P1.zip
```

`reassemble.py` 按编号拼接 `MapleDash-IL-Recovery-P1.zip.part00` 到 `.part09`，检查 SHA-256。

## 未完成
1. **没有完成高层 C# 反编译**；IL 文件不能视为可直接编译的 C#。
2. 尚未恢复完整类型签名、字段结构及异常处理区域到高层语法。
3. 原始 Unity 场景、Prefab、序列化引用和 Android IL2CPP 项目尚未恢复。
4. 未启动/验证服务端；`InProcessTransport` 不能直接证明已支持公网多人联机。
5. 未将这些文件与北斗现有业务逻辑进行合并。本目录仅用于独立逆向研究。

建议下一步在 Windows 环境使用 ILSpy/dnSpyEx 对原始 `Assembly-CSharp.dll` 进行 C# 导出，再验证编译与 Unity 工程依赖。反编译源码和游戏资产的分发须遵守权利人许可及相关法律。
