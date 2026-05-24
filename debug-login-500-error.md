# Debug Session: login-500-error
- **Status**: [OPEN]
- **Issue**: 登录页请求返回 `Request failed with status code 500`
- **Debug Server**: Pending
- **Log File**: .dbg/trae-debug-log-login-500-error.ndjson

## Reproduction Steps
1. 打开登录页。
2. 输入账号密码并提交登录。
3. 观察前端出现 `Request failed with status code 500`。

## Hypotheses & Verification
| ID | Hypothesis | Likelihood | Effort | Evidence |
|----|------------|------------|--------|----------|
| A | 登录接口内部抛出了未处理异常，可能发生在验证码开关读取或会话访问阶段 | High | Low | Pending |
| B | `AccessControlService.LoginAsync()` 在查询用户或构造返回上下文时触发了数据库/空引用异常 | High | Medium | Pending |
| C | 登录成功后的菜单或权限上下文序列化过程触发异常，导致接口最终返回 500 | Medium | Medium | Pending |
| D | 前端实际提交的数据与后端预期不一致，触发某个未覆盖的分支异常 | Medium | Low | Pending |
| E | 当前运行中的后端进程与源码不一致，命中了旧构建中的异常路径 | Medium | Low | Pending |

## Log Evidence
- Pending

## Verification Conclusion
- Pending
