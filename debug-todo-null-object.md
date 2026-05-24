# Debug Session: todo-null-object
- **Status**: [OPEN]
- **Issue**: 待办中心出现错误 "The requested operation requires an element of type 'Object', but the target element has type 'Null'."
- **Debug Server**: pending
- **Log File**: .dbg/trae-debug-log-todo-null-object.ndjson

## Reproduction Steps
1. 打开待办中心页面。
2. 执行触发报错的操作。
3. 观察前端报错与对应接口返回。

## Hypotheses & Verification
| ID | Hypothesis | Likelihood | Effort | Evidence |
|----|------------|------------|--------|----------|
| A | 待办详情或列表中的 JSON 字段被当作对象读取，但后端返回了 `null` | High | Low | Pending |
| B | 某个接口在 `JsonDocument` / `JsonElement` 上调用了对象访问方法，但源值为 `null` | High | Medium | Pending |
| C | 前端流程/待办页面在构造筛选参数或详情数据时，向需要对象的组件传入了 `null` | Medium | Low | Pending |
| D | 数据库中存在 `source_payload` / `ext_data` 等历史脏数据，反序列化后未做空值保护 | Medium | Medium | Pending |
| E | 待办中心调用流程中心返回了非预期空结构，随后在序列化或映射阶段抛错 | Low | Medium | Pending |

## Log Evidence
- Pending

## Verification Conclusion
- Pending
