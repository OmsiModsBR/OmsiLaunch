# CLI 範例

<!-- l10n: source=reference/cli-examples.md source-sha256=0b65dcecbe06ebef6e07dfc05ef1c1b5b95788ef6a892ebb05b51509ebb86733 -->
> 本頁為 OmsiLaunch 0.1.0-beta4 [英文原始頁面](../../../reference/cli-examples.md) 的翻譯。英文頁面為規範版本：若有出入，以英文頁面與程式碼為準。

OmsiLaunch `0.1.0-beta4` 的 `OmsiLaunch.exe` 最精簡且正確的呼叫方式，每個範例都附有預期的處理程序結束代碼，以及會變更與還原哪些內容的說明。除非另有說明，每個範例都從 OMSI 安裝根目錄（`<OMSI_PATH>`）執行；語法定義於 [CLI 參考](cli.md)，結束代碼定義於[結束代碼](exit-codes.md)。任何命令都可加上 `--json` 以取得結構化封套。

<a id="conventions"></a>
## 慣例

- **變更**：命令所變更的檔案或 OMSI 狀態。「工作階段 overlay（暫時覆蓋）」是指一個檔案會建立快照納入交易、在 OMSI 啟動前套用，並在工作階段結束時逐位元組還原。
- **還原**：工作階段結束時（正常停止、Ctrl+C、系統匣、`session stop`、`/observe-seconds`）或由復原程序撤銷的內容。
- 執行階段寫入（`time set`、`camera set`、`scripts variable set`、`vehicles spawn`）只會變更 OMSI 的記憶體；由於停止時 OMSI 會被終止，這些寫入永遠不會被還原。
- 預留位置：`<OMSI_PATH>` 是包含 OmsiLaunch 套件的 OMSI 2 安裝（例如 `C:\OMSI 2`）；`<OTHER_OMSI_PATH>` 是另一個安裝；`<SPEC_PATH>` 與 `<ITX_PATH>` 分別是您自己的 LaunchSpec 檔案與 Internet Textures（網路材質）設定檔；`<HANDLE>` 是前一個 `list` 或 `create` 命令印出的 handle（控制代碼），`<BASE64>` 是 Base64 像素資料。其他所有值都是可在標準 OMSI 2 安裝上運作的常值（`grundorf-quick` 是本頁定義的範例設定檔）。
- 包含空格的路徑請加上引號，且加引號的路徑不要以 `\` 結尾（Windows 引數解析會將 `\"` 轉為字面引號）：應寫 `"C:\OMSI 2"`，而非 `"C:\OMSI 2\"`。
- 本頁的每一行命令都會由文件檢查關卡解析（`tests/OmsiLaunch.DocumentationTests`，關卡 `examples`）；識別、探索、規劃與用戶端範例也已在真實安裝上執行過（`research/reports/OMSILAUNCH-BETA3-FINAL-DOCUMENTATION-AUDIT.md`）。

<a id="identity-and-discovery-no-session"></a>
## 識別與探索（不需工作階段）

```text
OmsiLaunch.exe /version
```
結束代碼 `0`。印出 `product`、`version`（`0.1.0-beta4`）、`protocol_version`（`0.1`）、`supported_family`。不變更任何內容。

```text
OmsiLaunch.exe profiles --json
```
結束代碼 `0`。列出支援的 `Omsi.exe` 雜湊及其驗證狀態。不變更任何內容。

```text
OmsiLaunch.exe capabilities --json
OmsiLaunch.exe help time
```
結束代碼 `0`。公開功能目錄；`help <family>` 可加以篩選。不變更任何內容。

```text
OmsiLaunch.exe detect
OmsiLaunch.exe
```
結束代碼 `0`（兩種形式完全相同）。回報執行中的 `Omsi.exe` 處理程序，以及是否有 OmsiLaunch 擁有者為此安裝回應。不變更任何內容。

```text
OmsiLaunch.exe /list:Maps
OmsiLaunch.exe /list:Entrypoints /map:maps\Grundorf\global.cfg
OmsiLaunch.exe /list:Repaints /vehicle-scope:Vehicles\MAN_SD200\MAN_SD77.bus
OmsiLaunch.exe "<OMSI_PATH>" /list:Situations
```
結束代碼 `0`（未知類別時為 `2`）。唯讀探索；會略過連接點循環。不變更任何內容。此處印出的 `Identity` 值，正是 `/map`、`/saved`、`/vehicle-scope` 與 `LaunchSpec` 所預期的確切字串（例如 `maps\Grundorf\global.cfg`、`situations\Linie 5.osn`）。

<a id="planning-and-validation"></a>
## 規劃與驗證

```text
OmsiLaunch.exe /new /map:maps\Grundorf\global.cfg /entrypoint-index:1 /plan
```
計畫為 `READY` 時結束代碼 `0`，為 `NOT RUNNABLE` 時為 `1`（例如 `OL_E_UNSUPPORTED_BUILD`、`OL_E_MAP_NOT_FOUND`、`OL_E_ENTRYPOINT_REQUIRED`）。不變更任何內容；不會啟動 OMSI。

```text
OmsiLaunch.exe /new /map:maps\Grundorf\global.cfg /entrypoint-index:1 /validate --json
```
結束代碼 `0`／`1`，同上；`/validate` 是 `/plan` 的別名。JSON 為原始的 `SessionPlan`（`TouchedFiles`、`PlannedMutations`、`Diagnostics`、`IsRunnable`）。

```text
OmsiLaunch.exe /new /map:maps\Grundorf\global.cfg /entrypoint-index:1 /date:2026-09-20 /plan
```
結束代碼 `1`。`/date`、`/time`、`/year`、`/weather*` 與玩家車輛旗標會被接受，但此組建不會套用；計畫帶有 `OL_E_CAPABILITY_UNAVAILABLE`，且不可執行。

```text
OmsiLaunch.exe /last /plan
```
結束代碼 `1`。`LAST_MAP_STATE` 在此設定檔上無法使用（`OL_E_CAPABILITY_UNAVAILABLE`）。

<a id="starting-sessions-owner-mode"></a>
## 啟動工作階段（擁有者模式）

```text
OmsiLaunch.exe /new /map:maps\Grundorf\global.cfg /entrypoint-index:1
```
工作階段以 `Completed` 結束時結束代碼 `0`；`Failed` 或計畫不可執行時為 `1`。變更：工作階段 overlay `GUI\NewSplashscreen_ENG.bmp` 與 `GUI\NewSplashscreen_<lang>.bmp`（受管理的啟動畫面為預設）、`closecheck` 處理、啟動交接。還原：工作階段結束時，逐位元組還原每個 overlay。主控台會保持附加，直到 OMSI 結束、系統匣「End session」獲得確認、用戶端傳送 `session stop`，或按下 Ctrl+C。

```text
OmsiLaunch.exe /new /map:maps\Grundorf\global.cfg /entrypoint-index:1 /observe-seconds:8
```
結束代碼 `0`。與上例相同，但工作階段會在到達 `Running` 後 8 s 停止（系統匣／管道停止時會更早）。供驗證指令碼使用。

```text
OmsiLaunch.exe "/saved:situations\Linie 5.osn"
```
結束代碼 `0`／`1`。SAVED_SITUATION：地圖、時間與位置來自 `.osn`（`situations\Linie 5.osn` 隨 OMSI 2 提供，在 Berlin-Spandau 以玩家巴士開始）。此值是 `/list:Situations` 印出的情境識別（相對於安裝、不區分大小寫）；像 `Linie 5.osn` 這樣的單純檔名無法解析（`OL_E_SITUATION_NOT_FOUND`，結束代碼 `1`）。`/map` 或 `/entrypoint-index` 與 `/saved` 併用時會被拒絕，結束代碼 `2`。變更與還原同 NEW_MAP。OMSI 本身會將情境的地圖記錄到 `options.cfg` 的 `[last_map]`；除非有 `/set` 對 `options.cfg` 進行 overlay，否則 OMSI 的這項寫入不會被還原（請見[交易與復原](../concepts/transactions-and-recovery.md)）。

```text
OmsiLaunch.exe /new /map:maps\Grundorf\global.cfg /entrypoint-index:1 /set:traffic.randomVehicles=150 /set:traffic.humans=200 /set:graphics.maxFPS=60
```
結束代碼 `0`。變更：`options.cfg`（工作階段 overlay，語意 token／向量修補；保留 CP1252 位元組）以及啟動畫面 overlay。還原：精確還原 `options.cfg` 與啟動畫面檔案（RV-005、RV-006）。`/set:graphics.texture=...` 的結束代碼為 `2`（`OL_E_SETTING_NOT_WRITABLE`）；`/set:foo=1` 的結束代碼為 `2`（`OL_E_UNKNOWN_SETTING`）。

```text
OmsiLaunch.exe /new /map:maps\Grundorf\global.cfg /entrypoint-index:1 /splash:Unset
```
結束代碼 `0`。變更：沒有啟動畫面 overlay；只有啟動交接與 `closecheck` 處理。還原：啟動畫面沒有需要還原的內容。

```text
OmsiLaunch.exe /new /map:maps\Grundorf\global.cfg /entrypoint-index:1 /splash:Managed /splash-language:PTB /splash-assets:.omsilaunch\assets\my-splash
```
結束代碼 `0`（目錄或 BMP 遺失、或不是 640x480 24 位元時，為 `1` 並帶 `OL_E_SESSION_PRESENTATION_INVALID`）。變更：來自自訂目錄的 `GUI\NewSplashscreen_ENG.bmp` 與 `GUI\NewSplashscreen_PTB.bmp`（工作階段 overlay）。還原：精確還原這兩個檔案。

```text
OmsiLaunch.exe /new /map:maps\Grundorf\global.cfg /entrypoint-index:1 /internet-textures:Disabled
```
結束代碼 `0`。變更：除了啟動畫面 overlay 外，磁碟上沒有其他變更；工作階段期間會抑制處理程序內下載器。

```text
OmsiLaunch.exe /new /map:maps\Grundorf\global.cfg /entrypoint-index:1 /internet-textures:Override /internet-textures-profile:<ITX_PATH>
```
結束代碼 `0`（省略設定檔時為 `2` 並帶 `OL_E_ITX_PROFILE_REQUIRED`；設定檔無效或目標位於 `Texture\` 之外時為 `1`）。變更：`Texture\standard.itx`（工作階段 overlay）；設定檔中列出的每個目標與 `Texture\standard.ipr` 都是工作階段刪除項目。還原：移除 overlay，還原被刪除的原始檔案；OMSI 在工作階段期間於這些路徑建立的檔案會作為工作階段副產物移除。

```text
OmsiLaunch.exe /new /map:maps\Grundorf\global.cfg /entrypoint-index:1 /startup-timeout:300
```
結束代碼 `0`。等待 `Running` 最長 300 s（+5 s），而非預設的 180 s。`/shutdown-timeout:60` 會被接受，但在此組建中沒有效果。

<a id="predefined-session-profile"></a>
### 預先定義的工作階段設定檔

設定檔 `<OMSI_PATH>\.omsilaunch\session-profiles\grundorf-quick\profile.yaml`：

```yaml
schema: omsilaunch.session-profile/v1
id: grundorf-quick
name: Grundorf quick start
author: Example
version: "1.0"
compatibility:
  maps:
    - maps\Grundorf\global.cfg
new:
  map: maps\Grundorf\global.cfg
  entrypoint-index: 1
presets:
  - index: 1
    id: low
    name: Low detail
    settings:
      graphics.maxFPS: 30
      graphics.tileDistance: 3
```

```text
OmsiLaunch.exe /predefined-profile:grundorf-quick /predefined-profile-index:1 /new
```
結束代碼 `0`。變更：`options.cfg`（預設組合設定，工作階段 overlay）以及啟動畫面 overlay。還原：全部。加上 `/map:...` 或 `/set:graphics.maxFPS=60` 時結束代碼為 `2`（`OL_E_SESSION_PROFILE_OVERRIDE_CONFLICT`）；省略 `/predefined-profile-index` 時結束代碼為 `2`（`OL_E_SESSION_PROFILE_PRESET_NOT_FOUND`）。套件內附的範例 `.omsilaunch\examples\session-profiles\rmg-leste\profile.yaml` 展示了完整的結構描述，但依出貨狀態，其 `new:` 區塊要求了此組建無法套用的 `date`、`time` 與 `weather`：以 `/new` 規劃它會得到 `NOT RUNNABLE`（`OL_E_CAPABILITY_UNAVAILABLE`）；使用前請移除這些 key。

<a id="launchspec-file"></a>
### LaunchSpec 檔案

```text
OmsiLaunch.exe /spec:.omsilaunch\examples\release-session.example.json /plan --json
OmsiLaunch.exe /spec:.omsilaunch\examples\release-session.example.json
```
結束代碼 `0`／`1`。套件內附的範例選擇 Grundorf、進入點索引 `1`、受管理的啟動畫面、原生 Internet Textures；`RootPath: "."` 會解析為可執行檔所在目錄。變更內容同明確指定的 NEW_MAP 範例。含未知屬性的規格結束代碼為 `2`（`OL_E_SPEC_UNKNOWN_PROPERTY: $.Path`）；檔案遺失時為 `6`（`OL_E_SPEC_NOT_FOUND`）；檔案超過 1 MiB 時為 `2`（`OL_E_SPEC_TOO_LARGE`）。

```text
OmsiLaunch.exe "<OTHER_OMSI_PATH>" /spec:<SPEC_PATH> /startup-timeout:120
```
結束代碼 `0`／`1`。明確的安裝 `<OTHER_OMSI_PATH>` 優先於規格的 `RootPath`；`/startup-timeout` 之所以覆寫規格的 `Behavior.StartupTimeoutSeconds`，只是因為有指定它。

<a id="silent-detached-start"></a>
### 靜默（分離）啟動

```text
OmsiLaunch.exe /silent /new /map:maps\Grundorf\global.cfg /entrypoint-index:1
```
`OmsiLaunchW.exe` 一啟動即以結束代碼 `0` 結束（`{"delegated": true, "host_process_id": <pid>}`）；若 `OmsiLaunchW.exe` 遺失（`OL_E_WINDOWS_HOST_MISSING`）或無法啟動，則為 `7`。啟動程式會立即返回，不會讓呼叫端的主控台或管道保持開啟：擷取其輸出的指令碼會立刻收到檔案結尾（執行階段封閉驗證 `T04`）。工作階段本身在 `OmsiLaunchW.exe` 中執行：沒有主控台輸出、失敗以訊息方塊顯示、可使用系統匣圖示。請以 `session status`、`events watch` 與 `.omsilaunch\diagnostics\<sessionId>-host.log` 檢查進度。完整參考：[OmsiLaunchW.exe](omsilaunchw.md)。

<a id="controlling-a-running-session-client-mode"></a>
## 控制執行中的工作階段（用戶端模式）

請在擁有者執行期間，從同一安裝目錄執行下列命令。沒有擁有者回應時，每個命令都以 `4`（`OL_E_NO_ACTIVE_SESSION`）結束，控制錯誤時以 `7` 結束。

```text
OmsiLaunch.exe session status --json
```
結束代碼 `0`。回傳 `SessionId`、`State`（`14` = `Running`）、`Diagnostics`、`RuntimeEvents`。不變更任何內容。

```text
OmsiLaunch.exe events read --json
OmsiLaunch.exe events watch
```
結束代碼 `0`（`events watch` 會執行到按下 Ctrl+C 為止）。有界的執行階段事件（`gameplay.entered`、D3D 生命週期事件……）。不變更任何內容。

```text
OmsiLaunch.exe session stop
```
結束代碼 `0`（`{"accepted": true, "session_id": "..."}`）。要求標準停止：OMSI 被終止、擁有者還原 overlay、日誌被刪除。用戶端會立即返回；擁有者處理程序在還原後結束。

<a id="runtime-reads"></a>
## 執行階段讀取

```text
OmsiLaunch.exe time get
OmsiLaunch.exe weather get
OmsiLaunch.exe weather actual get
OmsiLaunch.exe map get
OmsiLaunch.exe camera get
OmsiLaunch.exe player get
OmsiLaunch.exe timetable get
OmsiLaunch.exe timetable lines list
OmsiLaunch.exe drivers list
OmsiLaunch.exe tickets get
OmsiLaunch.exe vehicles summary
OmsiLaunch.exe humans summary
```
結束代碼 `0`，封套中帶有 `RuntimeCommandResult`（`Succeeded`、`Values`）。不變更任何內容。逾時 8 s（`OL_E_RUNTIME_REQUEST_TIMEOUT`，結束代碼 `7`）。

```text
OmsiLaunch.exe vehicles list
OmsiLaunch.exe vehicles get --handle=rv-000001
OmsiLaunch.exe hof get --handle=rv-000001
OmsiLaunch.exe constants list --handle=rv-000001
OmsiLaunch.exe constants get --handle=rv-000001 --name=antrieb_getr_version
OmsiLaunch.exe curves list --handle=rv-000001
OmsiLaunch.exe curves evaluate --handle=rv-000001 --name=retarder_stufe1 --x=0
OmsiLaunch.exe scripts variable list --handle=rv-000001
OmsiLaunch.exe scripts variable get --handle=rv-000001 --name=Refresh_Strings
OmsiLaunch.exe scripts string list --handle=rv-000001
OmsiLaunch.exe scripts string get --handle=rv-000001 --name=act_route
OmsiLaunch.exe humans list
OmsiLaunch.exe humans get --handle=hb-000001
```
結束代碼 `0`；缺少必要引數時為 `2`（`OL_E_RUNTIME_ARGUMENT_REQUIRED`，在送出要求前回報）；外掛程式拒絕要求時為 `7`：`OL_E_RUNTIME_OPERATION_FAILED`，具體原因在 `Values.detail` 中，例如對不再識別同一物件的 handle 回報 `OL_E_RUNTIME_OBJECT_HANDLE_STALE`，或 `OL_E_RUNTIME_CONSTANT_NOT_FOUND`。handle 屬於工作階段範圍，來自前一個 `list`。變數、常數與曲線名稱由各車輛模型定義：請從 `list` 結果取得。上述名稱是針對 `rv-000001` 列出的，它是 `situations\Linie 5.osn` 工作階段中的玩家巴士。不變更任何內容。

```text
OmsiLaunch.exe /runtime:timetable.track-entries.list
OmsiLaunch.exe /runtime:vehicle.constant.get /runtime-arg:handle=rv-000001 /runtime-arg:name=antrieb_getr_version
```
結束代碼 `0`。沒有階層式路由的操作（或任何路由）都可以透過操作 ID 呼叫。`timetable.track-entries.list` 是有界清單：在 `situations\Linie 5.osn` 上，它回傳了 825 個項目中的 137 個，並帶有 `truncated=true`（文件稽核的執行階段重新測試）。不變更任何內容。

<a id="runtime-writes"></a>
## 執行階段寫入

```text
OmsiLaunch.exe time set --minute=30
```
結束代碼 `0`。變更 OMSI 的記憶體內時鐘（已驗證：寫入、讀回，以及透過第二次 `time set` 還原）。停止時不會還原。

```text
OmsiLaunch.exe camera set --field_of_view=50
OmsiLaunch.exe camera lock --family=0 --preset=1
OmsiLaunch.exe camera unlock
```
結束代碼 `0`（`camera lock` 缺少 `--family` 時為 `2`）。變更工作階段的攝影機狀態。`camera lock` 需要 PlayerVehicle（例如 `/saved` 工作階段）；在無頭的 `/new` 工作階段中會失敗（`Values.detail` 中為 `OL_E_RUNTIME_PLAYER_VEHICLE_UNAVAILABLE`）。鎖定與解除鎖定已使用已儲存情境於執行階段驗證（family 0、2 與 1，並讀回攝影機狀態）。停止時不會還原；鎖定原則隨工作階段結束。

```text
OmsiLaunch.exe scripts variable set --handle=rv-000001 --name=Refresh_Strings --value=1
```
結束代碼 `0`（缺少 `handle`、`name` 或 `value` 時為 `2`）。變更該車輛的一個數值指令碼變數。不會還原。

```text
OmsiLaunch.exe weather set --wind_speed=5 --temperature=12 --cloud_type="Cumulus 1"
```
結束代碼 `0`。寫入 OMSI 的來源天氣記錄並停止即時 METAR 更新；OMSI 會在下一次未暫停的天氣更新週期，由此推導出霧、光線、實際風與濕度。寫入任何內容之前會先檢查每個值（`OL_E_RUNTIME_VALUE_OUT_OF_RANGE`，結束代碼 `7`）。像 `relative_humidity` 這類推導出的索引鍵會回傳 `OL_E_RUNTIME_SETTING_NOT_PERSISTENT`，並指出應改為設定的索引鍵。不會還原。EXPERIMENTAL：執行階段驗證待完成。

```text
OmsiLaunch.exe weather metar set --icao=EDDF
```
結束代碼 `0`。切換為該測站的即時 METAR 天氣；OMSI 會在下一次天氣更新時下載。EXPERIMENTAL：執行階段驗證待完成。

```text
OmsiLaunch.exe scripts string set --handle=rv-000001 --name=act_route --value=110
OmsiLaunch.exe scripts trigger --handle=rv-000001 --name=bus_doorfront0 --value=1
OmsiLaunch.exe scripts trigger --handle=rv-000001 --name=bus_doorfront0 --value=0
OmsiLaunch.exe scripts sound-trigger --handle=rv-000001 --trigger=ticket_print
```
結束代碼 `0`。`scripts string set` 透過 OMSI 自己的 `SetStringVariable` 執行，並回傳讀回的值；車輛指令碼未宣告的名稱會回傳 `OL_E_RUNTIME_STRING_VARIABLE_NOT_FOUND`（結束代碼 `7`）。`scripts trigger` 以 OMSI 輸入處理常式的方式觸發一個 trigger（`1` 按下、`0` 放開）；車輛指令碼會立即執行它。Trigger 與變數名稱由各個車輛模型定義；上面的名稱僅為範例。不會還原。EXPERIMENTAL：執行階段驗證待完成。

<a id="spawn"></a>
## 生成

```text
OmsiLaunch.exe vehicles spawn --model=Vehicles\MAN_SD200\MAN_SD77.bus
OmsiLaunch.exe vehicles place-random
```
結束代碼 `0`，並在 `Values` 中帶有新的 `rv-NNNNNN` handle（缺少 `--model` 時為 `2`；發生 `OL_E_MAKEVEHICLE_BUS_NOT_FOUND`、`OL_E_MAKEVEHICLE_DELTA_ZERO`、`OL_E_MAKEVEHICLE_DELTA_MULTIPLE`、`OL_E_RUNTIME_BUS_IDENTITY_INVALID` 時為 `7`）。用戶端逾時 30 s。變更道路車輛集合（新增一輛車）；不會指派玩家車輛。不會還原；該車輛會在停止時隨 OMSI 一同消失。

<a id="d3d-textures"></a>
## D3D 材質

```text
OmsiLaunch.exe /runtime:d3d.status
OmsiLaunch.exe /runtime:d3d.texture.create --width=8 --height=8 --format=A8R8G8B8 --levels=1
OmsiLaunch.exe /runtime:d3d.texture.describe --handle=<HANDLE> --level=0
OmsiLaunch.exe /runtime:d3d.texture.update --handle=<HANDLE> --level=0 --x=0 --y=0 --width=8 --height=8 --pixels_base64=<BASE64>
OmsiLaunch.exe /runtime:d3d.texture.release --handle=<HANDLE>
```
`<HANDLE>` 是 `create` 印出的 `handle` 值（`d3dtex-<session id>-<16 hex digits>`）。對 32 位元格式而言，`<BASE64>` 解碼後必須為 `width * height * 4` 位元組（8 x 8 x 4 = 256 位元組），且最多 48 KiB。結束代碼 `0`；缺少必要引數時為 `2`；發生 `OL_E_D3D_INVALID_TEXTURE_FORMAT`、`OL_E_D3D_INVALID_PIXEL_BUFFER`、`OL_E_D3D_RESOURCE_RELEASED`（第二次釋放，或釋放後 describe）、`OL_E_D3D_STALE_RESOURCE_HANDLE`（裝置重設前的 handle，或來自另一個工作階段的 handle）、`OL_E_D3D_RESET_IN_PROGRESS`、`OL_E_D3D_NOT_READY`、`OL_E_D3D_DEVICE_LOST` 時為 `7`。建立由工作階段擁有的 GPU 資源；可明確釋放，或在 OMSI 結束時釋放。不會動到任何檔案。

<a id="owner-side-single-runtime-operation"></a>
## 擁有者端的單一執行階段操作

```text
OmsiLaunch.exe /new /map:maps\Grundorf\global.cfg /entrypoint-index:1 /runtime:time.read /observe-seconds:5
```
結束代碼 `0`。啟動工作階段，在 `Running` 後執行一次 `time.read`（逾時 5 s），寫出 `.omsilaunch\diagnostics\<sessionId>-runtime-operation.json`，持續執行 5 s，然後停止並還原。執行階段失敗會以 `runtime_error` 印出，不會結束工作階段。

<a id="recovery"></a>
## 復原

```text
OmsiLaunch.exe /recovery-status --json
```
結束代碼 `0`：沒有日誌時為 `{"pending": false, ...}`，有日誌時為 `{"pending": true, "recovered": false}`。擁有者持有安裝期間，結束代碼為 `7`（`OL_E_INSTALLATION_BUSY`）。不變更任何內容。

```text
OmsiLaunch.exe /recover --json
```
沒有待處理項目或還原完成時結束代碼 `0`（`recovered: true`；`diagnostics` 可能包含 `restore.session-artifact-removed` 與 `OL_W_RESTORE_FOREIGN_FILE_RETAINED`）；日誌原本待處理且仍待處理時為 `8`（`OL_E_RECOVERY_BACKUP_CORRUPT`、`OL_E_RECOVERY_ABSENT_OWNERSHIP_MISMATCH`、`OL_E_RECOVERY_ABSENT_OWNERSHIP_UNVERIFIED`、`OL_E_RECOVERY_JOURNAL_REMOVE_FAILED`）；擁有者或日誌記錄的 OMSI 持有安裝期間為 `7`（`OL_E_INSTALLATION_BUSY`）；還原後的位元組驗證失敗時為 `10`（`OL_E_INTERNAL`）（`Restore hash mismatch` 或 `Restore presence mismatch`；日誌仍保持待處理）。變更：從 `.omsilaunch\backup\<sessionId>\` 還原每個日誌記錄的檔案（先驗證其 SHA-256），然後刪除日誌與備份目錄。日誌記錄的 `Omsi.exe` 仍存活時，會以 `OL_E_INSTALLATION_BUSY` 拒絕（執行階段封閉驗證 `S04`、`S04b`、`F01`）。

<a id="exit-code-quick-check-powershell"></a>
## 結束代碼快速檢查（PowerShell）

```powershell
& .\OmsiLaunch.exe /new /map:maps\Grundorf\global.cfg /entrypoint-index:1 /plan --json | Out-Null
$LASTEXITCODE   # 0 = READY, 1 = NOT RUNNABLE, 2 = bad arguments
```
