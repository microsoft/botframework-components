import sys, os, json, pdb, pyodbc, contextlib, csv
from metrics.code.config import Options
from metrics.code.collect_stat_func import StatData
from typing import List

def process_collect_stat(
    res_dir_list: List[str], 
    res_dir_description_list: List[str],
    out_dir: str, 
    bot_name : str,
    sql_server : str,
    sql_database : str,
    sql_user : str,
    sql_password : str,
    options: Options):
    '''
    collect stats for intent / entity of different results in res_dir_list
    '''
    input_fn_output_fn_list = [
        # TODO
        ('intent', options.dic['collect_stat']['intent']['Files'], 'intent_stat.json', 'intent_stat.txt'),
        ('entity', options.dic['collect_stat']['entity']['Files'], 'entity_stat.json', 'entity_stat.txt')
    ]
    for task_type, input_fn_list, output_json, output_txt in input_fn_output_fn_list:
        sd = StatData()
        for i in range(len(res_dir_list)):
            res_dir = res_dir_list[i]
            res_dir_desc = res_dir_description_list[i]
            for input_fn in input_fn_list:
                json_obj = json.load(open(os.path.join(res_dir, input_fn), 'r'))
                sd.add_json_obj(
                    json_obj,
                    options.dic["collect_stat"][task_type]['LabelSet'],
                    options.dic["collect_stat"][task_type]['LabelKey'])
        
        if os.path.exists(out_dir) is False:
            os.mkdir(out_dir)

        out_path = os.path.join(out_dir, output_json)
        json.dump(sd.data, open(out_path, 'w'), indent=2)
        print("%s written"%out_path)

        out_path = os.path.join(out_dir, output_txt)
        f = open(out_path, 'w')
        f.write(sd.get_table_str(options.dic["collect_stat"][task_type]['PrintLabelOrder']))
        f.close()
        print("%s written"%out_path)

        table_name = bot_name + "_" + task_type + "_Results"

        send_intent_stats_to_sql(sql_server, sql_database, sql_user, sql_password, out_path, table_name, task_type)

    return

def send_intent_stats_to_sql(
    sql_server : str,
    sql_database : str,
    sql_user : str,
    sql_password : str,
    path_to_csv : str,
    table_name : str,
    task_type : str) :

    sql_string = "INSERT INTO [dbo].[{}] (Timestamp, Intent, F1, Precision, Recall, NumLabels, Tp, Fp, Fn) VALUES ('{}', '{}', {}, {}, {}, {}, {}, {}, {})"
    
    if task_type == 'entity':
        sql_string = "INSERT INTO [dbo].[{}] (Timestamp, Entity, F1, Precision, Recall, NumLabels, Tp, Fp, Fn) VALUES ('{}', '{}', {}, {}, {}, {}, {}, {}, {})"

    connection_string = "DRIVER={{ODBC Driver 17 for SQL Server}};SERVER=tcp:{};DATABASE={};UID={};PWD={}"

    with contextlib.closing(pyodbc.connect(connection_string.format(sql_server, sql_database, sql_user, sql_password))) as sql_connection:
        with contextlib.closing(sql_connection.cursor()) as sql_cursor:
            with open(path_to_csv, newline='') as csvfile:
                reader = csv.reader(csvfile, delimiter=',')
                for row in reader:
                    if row[0] == 'Timestamp':
                        continue
                    sql_cursor.execute(sql_string.format(table_name, row[0], row[1], row[2], row[3], row[4], row[5], row[6], row[7], row[8]))
        sql_connection.commit()
    
    return