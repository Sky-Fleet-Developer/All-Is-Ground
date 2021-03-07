<html><body>
<meta charset="UTF-8">
<?php

include ("database.php");
$method = $_GET ["method"];

switch ($method) {
    case "Auth":
        $name = $_GET["name"];
        $password = $_GET["password"];

        $account = DB::GetFetchArray($db_conn, "SELECT id, password, experience, free_experience, garage_set FROM accounts WHERE name = '$name' LIMIT 1");

        if ($account)
		{
			$account_id = $account["id"];
            $account_password = $account["password"];

			$sault = "$password$account_id";
            $pass = hash('sha512', $sault);

			if ($pass != $account_password) 
			{
                echo "<start>auth=error&error=Не верный логин или пароль</start>";
                exit;
            } 
			else 
			{
				$account_experience = $account["experience"];
				$account_free_experience = $account["free_experience"];
				$account_set = $account["garage_set"];

				echo "<start>auth=correct&name=$name&experience=$account_experience&free_exp=$account_free_experience&set=$account_set</start>";
			}
        } 
		else 
		{
            echo "<start>error=Аккаунт не найден</start>";
            exit;
        }
        break;

    case "Registration":
        $name = $_GET["name"];
        $password = $_GET["password"];
        $id = $_GET["id"];

        $account = DB::GetFetchArray($db_conn, "SELECT id FROM accounts WHERE name = '$name' LIMIT 1");
        if ($account) 
		{
            echo "<start>auth=error&error=Имя занято другим игроком</start>";
            exit; // Ник занят
        } 
		else
		{
            DB::SendQuery($db_conn, "INSERT INTO accounts(`id`,`name`) VALUES ('$id','$name')"); // Создаем новый аккаунт
            $newAccount = DB::GetFetchArray($db_conn, "SELECT id FROM accounts WHERE name = '$name' LIMIT 1"); // Ищем только что созданный аккаунт
			if($newAccount)
			{
				$account_id = $newAccount["id"];
				$sault = "$password$account_id";
				$pass = hash('sha512', $sault);

				DB::SendQuery($db_conn, "UPDATE `accounts` SET `password` = '$pass' WHERE id ='$account_id'");// Сохраняем пароль

				echo "<start>auth=correct&name=$name&experience=0</start>";
			}
			else
			{
				echo "<start>auth=error&error=Не удалось создать аккаунт</start>";
			}
        }
        break;

    
    case "AddExperience":
        $name = $_GET["name"];
        $addexp = $_GET["exp"];
        $account = DB::GetFetchArray($db_conn, "SELECT experience, free_experience FROM accounts WHERE name = '$name' LIMIT 1");

        if ($account) 
		{
            $account_id = $account["id"];
            $experience = $account["experience"];
            $free_experience = $account["free_experience"];
            $newexp = $experience + $addexp;
			$newfreeexp = $free_experience + $addexp;
            DB::SendQuery($db_conn, "UPDATE `accounts` SET `experience`, `free_experience` = '$newexp', '$newfreeexp' WHERE name ='$name'");
            echo "<start>experience=$newexp</start>";
        } 
		else
		{
            echo "<start>error=account is ont exist</start>";
        }
        break;

	case "GetExperience":
        $name = $_GET["name"];
        $account = DB::GetFetchArray($db_conn, "SELECT experience, free_experience FROM accounts WHERE name = '$name' LIMIT 1");

        if ($account) 
		{
            $experience = $account["experience"];
            $free_experience = $account["free_experience"];
            echo "<start>experience=$experience&free_experience=$free_experience</start>";
        } 
		else
		{
            echo "<start>error=account is ont exist</start>";
        }
        break;

	case "GetItemsCosts":
	    $ship = $_GET["ask"];
		$ask = DB::GetFetchAll($db_conn, "SELECT name, cost FROM costs WHERE name LIKE '$ship%'");

		$answer = "";//implode(', ', $ask);

		foreach($ask as $hit)
		{
			$answer = $answer . $hit[0] . "=" . $hit[1] . ";";
		}

		echo "<start>$answer</start>";
		break;

	case "SetItemsCosts":
		$all = $_GET["items"];
		$items = explode(",", $all);
		$report = "";
		foreach($items as $item)
		{
			$keyvalue = explode(":", $item);
			$key = $keyvalue[0];
			$value = $keyvalue[1];
			$old = DB::GetFetchArray($db_conn, "SELECT name, cost FROM `costs` WHERE name = '$key' LIMIT 1");
			if($old["name"] == $key)
			{
				DB::SendQuery($db_conn, "UPDATE `costs` SET `cost` = '$value' WHERE name ='$key'");
				$report = $report . "update: \"$key\"=$value;";
			}
			else
			{
				DB::SendQuery($db_conn, "INSERT INTO costs(`name`, `cost`) VALUES ('$key','$value')");
				$report = $report . "insert: \"$key\"=$value;";
			}
		}
		echo "<start>report: $report</start>";
		break;

	case "GetShipsCosts":
		$quest = $_GET["ask"];
		$items = explode(",", $quest);
		$count = count($items);
		$answer = "";
		for($i = 0; $i < $count; $i++)
		{
			if($items[$i] != "")
			{
				$item = $items[$i];
				$cost = DB::GetFetchArray($db_conn, "SELECT cost FROM `costs` WHERE name = '$item' LIMIT 1");
				$answer = $answer . $item . "=" . $cost[0] . ";";
			}
		}
		echo "<start>$answer</start>";
		break;

	case "Explore":
		$name = $_GET["name"];
		$item = $_GET["item"];
        $account = DB::GetFetchArray($db_conn, "SELECT free_experience, garage_set FROM accounts WHERE name = '$name' LIMIT 1");
		$garage_set	= $account["garage_set"];
		$free_experience = $account["free_experience"];

		$_item = DB::GetFetchArray($db_conn, "SELECT name, cost from costs WHERE name = '$item' LIMIT 1");
		$_name = $_item["name"];
		$cost = $_item["cost"];

		if($_name != $item)
		{
			echo "<start>result=error&error=item was not found.name=$_name. search=$item</start>";
			exit;
		}

		if($free_experience > $cost)
		{
			$type = $_GET["type"];
			if($type == "ship")
			{
				$new_set = $garage_set . $item . ":standart;";
				$new_free_exp = $free_experience - $cost;
				DB::SendQuery($db_conn, "UPDATE `accounts` SET `garage_set` = '$new_set' WHERE name ='$name'");
				DB::SendQuery($db_conn, "UPDATE `accounts` SET `free_experience` = '$new_free_exp'  WHERE name ='$name'");
				echo "<start>result=correct&free_experience=$new_free_exp</start>";
			}
			if($type == "item")
			{
				$ship = explode(".", $item)[0];
				$itemN = explode(".", $item);
				$itemName = "";
				if(count($itemN) == 3)
					$itemName = $itemN[1] . "." . $itemN[2];
				else
					$itemName = $itemN[1];


				$setexplode = explode(";", $garage_set);
				for($i = 0; $i < count($setexplode); $i++) //перебираем машинки
				{
					$shipset = explode(":", $setexplode[$i]); //разбиваем сет машинки на имя и модули
					if($shipset[0] == $ship) //если имя нужное
					{
						if($shipset[1] == "standart")
						{
							$shipset[1] = $itemName;
						}
						else
						{
							$shipset[1] = $shipset[1] . "," . $itemName; //прибавляем к модулям 
						}
						$setexplode[$i] = $shipset[0] . ":" . $shipset[1]; //собираем сет машинки обратно из имени и модулей
						$garage_set = implode(";", $setexplode); //собираем гараж из сетов машинок
						$new_free_exp = $free_experience - $cost;
						DB::SendQuery($db_conn, "UPDATE `accounts` SET `garage_set` = '$garage_set' WHERE name ='$name'");
						DB::SendQuery($db_conn, "UPDATE `accounts` SET `free_experience` = '$new_free_exp'  WHERE name ='$name'");
						$ss = $setexplode[$i];
						echo "<start>result=correct&free_experience=$new_free_exp&set=$garage_set&shipSet=$ss</start>";
					}
				}
			}
			echo "<start>result=error&error=unexpected type</start>";
		}
		else
		{
			echo "<start>result=error&error=low experience</start>";
		}

		break;
}
?>
</body></html>