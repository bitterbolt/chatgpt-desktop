# ChatGPT Desktop

Настольная оболочка (WinForms + WebView2, .NET 8, `net8.0-windows`) для основных
чат-нейросетей: ChatGPT, Claude, Gemini, DeepSeek, Grok, Copilot.

## Возможности
- Стартовый оверлей с круглыми иконками сервисов.
- Авто-скрывающаяся верхняя панель инструментов и адресная строка.
- Полноэкранный режим (F11 / F10 / Esc).
- Опциональный «обход блокировок» сменой DNS (`Win32_NetworkAdapterConfiguration`) —
  **требует прав администратора**.
- Иконки, скрипты и `WebView2Loader.dll` встроены как `EmbeddedResource`; loader
  извлекается во временную папку в рантайме.

## Требования
- Windows 10/11.
- .NET 8 SDK (сборка) и среда выполнения WebView2 (в Windows 11 присутствует по умолчанию).

## Сборка
```
dotnet build -c Release
```
Публикация single-file: `dotnet publish -c Release -r win-x64`.

Артефакты сборки (`bin/`, `obj/`) не версионируются (см. `.gitignore`).
