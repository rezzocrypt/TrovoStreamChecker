# StreamChecker

Показывает, какие стримеры сейчас онлайн на поддерживаемых платформах: статус трансляции, число зрителей, игру, название стрима, аватар стримера и логотип платформы.

Проверка идёт через публичные источники платформ

## Платформы
- **Twitch** — публичный GraphQL-эндпоинт `gql.twitch.tv`
- **Kick** — публичный API `kick.com/api/v2`
- **VK Play** — публичный API `api.live.vkvideo.ru` через локальный прокси `/vkplay-api` (в `vite.config.js`): работают `npm run dev` и `npm run preview`. При развёртывании статики на другом сервере нужно настроить аналогичный прокси на `https://api.live.vkvideo.ru/v1`.

Чтобы добавить новую платформу: создайте модуль по образцу `src/platforms/twitch.js` и добавьте его в массив `platforms` в `src/platforms/index.js`.

## Запуск

```
npm install
npm run dev
```

## Сборка

```
npm run build
```

Результат в `dist/`. Статику можно отдавать любым веб-сервером:

```
npm run preview
```

## Данные

Каналы и настройки хранятся в localStorage браузера. Импорт/экспорт работает через JSON-файл структуры:

```json
[
  { "platform": "twitch", "name": "shroud" },
  { "platform": "kick", "name": "destiny" }
]
```