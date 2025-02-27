# ИНСТРУКЦИЯ, ЧИТАЙ СУКА
Важно! Не закрывайте программу до надписи `Ready`
- Для работы программы требуется https://dotnetwebsite.azurewebsites.net/en-us/download/dotnet/8.0
- Скачать последнюю версию программы из релиза
- Настроить конфиг `config.json` (Содержит комментарии)
- ВСЕ СЕРВЕРА ДОЛЖНЫ БЫТЬ ПРОПИСАНЫ!!!
- Запустить `DbUpdaterApp.exe` и подождать надписи `Ready`
- Процесс `BansSql construct` и `CommsSql construct...` - может занять несколько минут (зависит от кол-ва ваших банов),  так что не спешите закрывать программу
- Создастся файл `import.sql`, его нужно будет импортировать в базу данных
## Как оно работает (Что-б знали)
- Прога считывает все данные с базы данных
- Помещает запрос в файл `import.sql`, он в свою очередь:
- Помечает прошлую базу как old_
- Создаёт новую базу данных с новой структурой
- Добавляет туда сервера из конфига
## ЕСЛИ ЧТО ТО ПОШЛО НЕ ТАК, УДАЛИТЕ БАЗЫ iks_ (НЕ old_iks), НАПИШИТЕ МНЕ В ДС И ПРОПИШИТЕ ЗАПРОС НИЖЕ:
```sql
rename table old_iks_admins to iks_admins;
rename table old_iks_gags to iks_gags;
rename table old_iks_mutes to iks_mutes;
rename table old_iks_groups to iks_groups;
rename table old_iks_bans to iks_bans;
```

