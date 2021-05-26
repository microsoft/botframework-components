import sys, os, json, pdb
from metrics.code.config import Options
from metrics.code.plot_func import BarData, RocData, ConfusionMatrixData, \
    plot_bar_data, plot_roc_data, plot_confusion_matrix_data
from metrics.code.collect_stat_func import StatData
from typing import List

def parse_args(args: List[str]) -> (List[str],str):
    """
    args:
      - could be: code/main.py -o [out_dir]
                               -i [res_dir_1] [res_dir_2] ...
                               -i2 [annot_data_1 or na] [annot_data_2 or na] ...
                               -d [res_dir_1_description] [res_dir_2_description] ...
      - or w/o args: code/main.py 
    """
    if len(args) == 1:
        data_dir = os.path.join(
            os.path.dirname(os.path.dirname(os.path.realpath(__file__))),
            'data')

        res_dir_list = [
            os.path.join(data_dir, 'benchmark_res/train_as_test'),
            os.path.join(data_dir, 'benchmark_res/test_as_test'),
        ]

        annot_data_list = [
            os.path.join(data_dir, 'annotated_data/train_as_test.json'),
            os.path.join(data_dir, 'annotated_data/test_as_test.json'),
        ]

        res_dir_description_list = [
            'train_as_test',
            'test_as_test',
        ]

        out_dir = os.path.join(data_dir, 'out_dir')
    else:
        out_dir = args[args.index('-o') + 1]
        res_dir_list = args[args.index('-i') + 1 : args.index('-i2')]
        annot_data_list = args[args.index('-i2') + 1 : args.index('-d')]
        res_dir_description_list = args[args.index('-d') + 1 :]
        pdb.set_trace()
        
    return res_dir_list, annot_data_list, out_dir, res_dir_description_list

def process_plot_bars_one_fig(
    task_type: str,
    res_dir_list: List[str],
    res_dir_description_list: List[str], 
    out_dir: str, 
    options: Options,
    input_fn: str,
    output_fig: str):

    bd = BarData()

    for res_dir_idx in range(len(res_dir_list)):
        res_dir = res_dir_list[res_dir_idx]
        res_dir_description = res_dir_description_list[res_dir_idx]

        res_contents = json.load(open(os.path.join(res_dir, input_fn), 'r'))

        if task_type=='intent':
            bd.add_ClassifierMetrics(
                res_contents,
                res_dir_description,
                options.dic['bars']['intent']['ComparisonType_Thre_Set'],
                options.dic['bars']['intent']['NameSet'])
        elif task_type=='entity':
            bd.add_ExtractorsMetrics(
                res_contents,
                res_dir_description,
                options.dic['bars']['entity']['MetricsType_Set'],
                options.dic['bars']['entity']['NameSet']
            )
        else:
            print('unknown task type: %s'%task_type)
            raise Exception

    if os.path.exists(out_dir) is False: os.mkdir(out_dir)
    plot_bar_data(bd, os.path.join(out_dir, output_fig))

    return

def process_plot_roc_one_fig(
    task_type: str,
    res_dir_list: List[str],
    res_dir_description_list: List[str], 
    out_dir: str, 
    options: Options,
    input_fn: str,
    output_fig: str):

    rd = RocData()

    for res_dir_idx in range(len(res_dir_list)):
        res_dir = res_dir_list[res_dir_idx]
        res_dir_description = res_dir_description_list[res_dir_idx]

        res_contents = json.load(open(os.path.join(res_dir, input_fn), 'r'))

        if task_type=='intent':
            rd.add_ClassifierMetrics(
                res_contents,
                res_dir_description,
                options.dic['roc']['intent']['NameSet'],
                x_axis=options.dic['roc']['intent']['x_axis'],
                y_axis=options.dic['roc']['intent']['y_axis'])
        else:
            print('unknown task type: %s'%task_type)
            raise Exception
    
    if os.path.exists(out_dir) is False: os.mkdir(out_dir)
    plot_roc_data(
        rd,
        os.path.join(out_dir, output_fig),
        x_axis=options.dic['roc']['intent']['x_axis'],
        y_axis=options.dic['roc']['intent']['y_axis'])

    return 

def process_plot_confusion_one_fig(
    task_type: str,
    res_dir_list: List[str],
    annot_data_list: List[str],
    res_dir_description_list: List[str],
    out_dir: str,
    options: Options,
    input_fn_list: List[str],
    output_fig: str):

    cd = ConfusionMatrixData()

    assert len(res_dir_list)==len(annot_data_list)

    for i in range(len(res_dir_list)):  # e.g. train_as_test, test_as_test
        res_dir = res_dir_list[i]
        res_dir_description = res_dir_description_list[i]
        annot_data = annot_data_list[i]
        annot_data = json.load(open(annot_data, 'r'))

        for input_fn_idx in range(len(input_fn_list)):  # e.g. thre 0, 0.5 etc
            input_fn = input_fn_list[input_fn_idx]
            res_contents = json.load(open(os.path.join(res_dir, input_fn), 'r'))

            if task_type=='intent':
                # TODO cd.add_ClassifierErrorAnalysisReport()
                cd.add_ClassifierErrorAnalysisReport(
                    res_dir_description + ', intent, ' + input_fn,
                    annot_data,
                    res_contents
                )
            elif task_type=='entity':
                # TODO cd.add_EntitiesErrorAnalysisReport()
                pass
            else:
                print('unknown task type: %s'%task_type)
                raise Exception
    
    if os.path.exists(out_dir) is False: os.mkdir(out_dir)
    plot_confusion_matrix_data(
        cd,
        os.path.join(out_dir, output_fig)
    )

    return

def process_plot_bars(res_dir_list: List[str], res_dir_description_list: List[str], out_dir: str, options: Options):
    """
    plot prec / recall / f1 bars for intent / entities of different results in res_dir_list
    """
    input_fn_output_fig_list = [
        ('intent', 'ClassifiersMetrics.json', 'bars_intent.png'),
        ('entity', 'ExtractorsMetrics.json', 'bars_entities.png')
    ]
    for task_type, input_fn, output_fig in input_fn_output_fig_list:
        process_plot_bars_one_fig(
            task_type,
            res_dir_list,
            res_dir_description_list,
            out_dir,
            options,
            input_fn,
            output_fig
        )
    return

def process_plot_roc(res_dir_list: List[str], res_dir_description_list: List[str], out_dir: str, options: Options):
    '''
    plot roc for intent of different results in res_dir_list
    '''
    input_fn_output_fig_list = [
        ('intent', 'ClassifiersMetrics.json', 'roc_intent.png')
    ]
    for task_type, input_fn, output_fig in input_fn_output_fig_list:
        process_plot_roc_one_fig(
            task_type,
            res_dir_list,
            res_dir_description_list,
            out_dir,
            options,
            input_fn,
            output_fig
        )
    return

def process_plot_confusion_matrix(
    res_dir_list: List[str], 
    annot_data_list: List[str],
    res_dir_description_list: List[str],
    out_dir: str, 
    options: Options):
    '''
    plot confusion matrix for intent / entity of different results in res_dir_list
    '''
    input_fn_output_fig_list = [
        ('intent', options.dic['confusion']['intent']['Files'], 'confusion_intent.png'),
        # TODO
        # ('entity', options.dic['confusion']['entity']['Files'], 'confusion_entity.png')
    ]
    for task_type, input_fn_list, output_fig in input_fn_output_fig_list:
        process_plot_confusion_one_fig(
            task_type,
            res_dir_list,
            annot_data_list,
            res_dir_description_list,
            out_dir,
            options,
            input_fn_list,
            output_fig
        )

    return

def process_collect_stat(
    res_dir_list: List[str], 
    res_dir_description_list: List[str],
    out_dir: str, 
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
                json_obj_label = "%s, %s, %s"%(res_dir_desc, task_type, input_fn)
                sd.add_json_obj(
                    json_obj,
                    json_obj_label,
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

    return

def process(
    res_dir_list: List[str],
    annot_data_list: List[str],
    res_dir_description_list: List[str],
    out_dir: str,
    options: Options):
    """
    process metric results according to options, and plot and save figs to out_dir
    """
    if options.plot_bars():
        process_plot_bars(res_dir_list, res_dir_description_list, out_dir, options)

    if options.plot_roc():
        process_plot_roc(res_dir_list, res_dir_description_list, out_dir, options)
    
    if options.plot_confusion_matrix():
        process_plot_confusion_matrix(
            res_dir_list,
            annot_data_list,
            res_dir_description_list,
            out_dir,
            options)
    
    if options.collect_stat():
        process_collect_stat(
            res_dir_list,
            res_dir_description_list,
            out_dir,
            options)

    return

"""
Usage:
  python metrics/code/main.py -o [out_dir]
                      -i [res_dir_1] [res_dir_2] ...
                      -i2 [annot_data_1 or na] [annot_data_2 or na] ...
                      -d [res_dir_1_description] [res_dir_2_description] ...
                      
  - the plot configs are in config.py
  - -o, -i, -i2, -d orders matter
  - -i2 is needed for confusion matrix plot. otherwise write -i2 na na ...
"""
def main(args: List[str]):
    res_dir_list, annot_data_list, out_dir, res_dir_description_list = parse_args(args)

    options = Options()
    process(res_dir_list, annot_data_list, res_dir_description_list, out_dir, options)
    return

if __name__ == '__main__':
    args = sys.argv
    main(args)