<html><body>
<?php

include ("config.php"); // подключаем скрипт содержащий информацию о БД

$db_conn = mysqli_connect($db_host, $db_user, $db_pass)  or die (mysql_error()); // коннектимся к БД
$db_sellect = mysqli_select_db ($db_conn, $db_name); // выбираем нужную БД

//if (!db_sellect) { echo "Data Base sellection - error"; exit; } // Если такой БД не существует

mysqli_set_charset($db_conn, "UTF-8"); // Установить UTF-8 кодировку
mysqli_query($db_conn, "set names 'UTF8'"); // Установить UTF-8 кодировку

class DB // класс DataBase
{
    public static function SendQuery ($db, $query) // Функция обновления инфы БД
    {
        return mysqli_query ($db, $query);
    }

    public static function GetFetchArray ($db, $query) // Функция чтения с БД
    {
        return mysqli_fetch_array (self::SendQuery ($db, $query));
    }
    
    public static function GetFetchAll ($db, $query)
    {
        return mysqli_fetch_all (self::SendQuery ($db, $query));
    }
}

?>
</body>
</html>