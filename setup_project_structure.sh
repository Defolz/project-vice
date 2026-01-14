#!/bin/bash

# Скрипт для создания структуры папок проекта
# Запускать из корня проекта (рядом с Assets/)

set -e  # Прервать выполнение при первой ошибке

echo "Создание структуры проекта..."

mkdir -p Assets/Code/Core/ECS/{Components,Systems,Jobs}
mkdir -p Assets/Code/Core/Gameplay/{Events,ScriptableObjects}
mkdir -p Assets/Code/Core/Infrastructure/{Editor,Utilities}
mkdir -p Assets/Code/World/Generation
mkdir -p Assets/Code/UI/Scripts
mkdir -p Assets/Resources/Data
mkdir -p Assets/Scenes

echo "✅ Структура успешно создана!"
echo "Рекомендуется добавить .meta-файлы (если Unity ещё не запущена):"
echo "   - Открой проект в Unity Editor хотя бы один раз"
echo "   - Или используй скрипт для генерации .meta (по желанию)"