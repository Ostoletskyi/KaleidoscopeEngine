# ASSISTANT GUIDE

## Цель
Файл-памятка для AI/Codex при работе с проектом.

## Что читать сначала
- README.md проекта.
- Конфигурационные файлы в корне проекта.
- Входные точки приложения: файлы запуска, основной модуль, package.json/csproj/sln.

## Где конфиг
- Utility config: `C:\Projects\CLIPS\ProjectOpsUtility\config\config.json`
- Codex config: `%USERPROFILE%\.codex\config.toml` или `<ProjectPath>\.codex\config.toml`.

## Какие папки не трогать
- .git, node_modules, bin, obj, dist, build, .vs, .idea, .vscode
- Любые каталоги сборки, кеша и внешних зависимостей без явной необходимости.

## Где логи
- Utility logs: `C:\Projects\CLIPS\ProjectOpsUtility\logs`

## Как запускать проект
- Использовать штатные скрипты/команды проекта.
- Для работы внутри проекта можно открыть PowerShell из меню утилиты.

## Правила работы
- Сначала анализировать состояние репозитория.
- Не выполнять опасные git-действия без backup branch.
- При неясном назначении файла не менять его без дополнительной проверки.
