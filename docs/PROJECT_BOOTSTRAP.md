# Project Bootstrap Report

## Что создано

Подготовлена стартовая инфраструктура Unity-проекта для музыкального клипа на URP без изменения существующих настроек рендера и без импорта сторонних пакетов.

## Какие папки созданы

- `Assets/Audio`
- `Assets/Editor`
- `Assets/Materials`
- `Assets/Prefabs`
- `Assets/Scripts`
- `Assets/Scripts/Core`
- `Assets/Shaders`
- `Assets/Textures`
- `Assets/UI`
- `Assets/VFX`
- `Docs`

## Какие файлы созданы

- `.gitignore`
- `Assets/Editor/ProjectBootstrapEditor.cs`
- `Assets/Scripts/Core/BeatManager.cs`
- `Assets/Scripts/Core/MusicPlayerController.cs`
- `Assets/Scripts/Core/SceneBootstrap.cs`
- `Docs/PROJECT_BOOTSTRAP.md`

## Какие файлы обновлены

- `README.md`

## Сцена

- Используется `Assets/Scenes/Main.unity`.
- Если сцена уже существует, bootstrap открывает её, при необходимости гарантирует наличие `Main Camera` и `Directional Light`, затем сохраняет без агрессивной очистки.
- Если сцена отсутствует, bootstrap создаёт новую сцену и оставляет только базовые стартовые объекты.

## Как запустить bootstrap в Unity

1. Откройте проект в Unity 2022.3 LTS.
2. Дождитесь импорта новых файлов и компиляции скриптов.
3. В верхнем меню выберите `Tools > Project Bootstrap > Run Bootstrap`.
4. После выполнения проверьте `Console` для подробного лога.

## Что должно получиться

- В `Assets` существует базовая структура папок для клипа.
- В проекте есть каркасные core-скрипты для дальнейшей логики.
- `Assets/Scenes/Main.unity` находится в безопасном стартовом состоянии.
