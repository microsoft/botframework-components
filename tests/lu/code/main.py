from sys import argv, exit
import getopt
import re
import json
import os.path
import os
import datetime
import shutil

# Tries to get LUIS AppId from settings
def get_app_id(model_folder):
    # Searches for settings json file
    for candidate in os.listdir(model_folder):
        if candidate.endswith(".json"):
            with open(os.path.join(model_folder, candidate), "r") as f:
                settings = json.load(f)
                if "luis" in settings:
                    try:
                        appId = list(settings["luis"].values())[0]["appId"]
                        return appId 
                    except:
                        appId = list(settings["luis"].values())[0]
                        return appId
    return ''

def run_cmd(cmd):
    print("Running command: {}".format(cmd))
    os.system(cmd)

def GetConfig(config, k):
    '''
    Get a value of key k from luis config. If value is 'null' or None,
    it means it's a secret and needs to be specified locally
    '''
    if k not in config:
        raise Exception("unknown key=%s for luis config"%k)

    v = config[k]
    if v is None or v=='null':
        v = input('luis config has %s=%s, please specify the value (or update config file locally):'%(k, str(v)))
        config[k] = v

    return v

def build(luis_config, output_folder):
    print('Building model...')
    model_folder = os.path.join(output_folder, "model")
    os.makedirs(model_folder)
    run_cmd("bf luis:build --botName {} --authoringKey {} --region {} --in {} --out {} --dialog={} --suffix {} --log --endpoint {}".format(
        luis_config["BotName"],
        GetConfig(luis_config, "AuthoringKey"),
        luis_config["Region"],
        luis_config["TrainLu"],
        model_folder,
        luis_config["Dialog"],
        luis_config["Suffix"],
        GetConfig(luis_config, "AuthoringEndpoint")))

    model_json_file = os.path.join(model_folder, "model.json")
    run_cmd("bf luis:convert --in {} --out={}".format(
        luis_config["TrainLu"],
        model_json_file
    ))

    return get_app_id(model_folder), model_json_file

def test(output_folder, pred_key, pred_endpoint, app_id, test_file, test_name):
    print("Testing model...")
    test_file_name = os.path.join(output_folder, test_name + ".out")
    run_cmd("bf luis:test --in {} --subscriptionKey={} --appId={} -o {} --endpoint={} ".format(
        test_file,
        pred_key,
        app_id,
        test_file_name,
        pred_endpoint
    ))

    model_json_file = os.path.join(output_folder, "test.json")
    run_cmd("bf luis:convert --in {} --out={}".format(
        test_file,
        model_json_file
    ))

    return test_file_name + ".log", model_json_file

def get_pred_entities(obj):
    def create_entity_obj(inst):
        entities = []
        for entity_type in inst:
            for entity in inst[entity_type]:
                entities += [{
                    "entity": entity_type,
                    "startPos": entity["startIndex"],
                    "endPos": int(entity["startIndex"]) + int(entity["length"]) - 1,
                    "score": entity.get("score", -1)}]
        return entities

    entities = []
    if type(obj) is list:
        for element in obj:
            entities += get_pred_entities(element)
    elif type(obj) is dict:
        for entity_type in obj:
            if entity_type == "$instance":
                entities += create_entity_obj(obj[entity_type])
            else:
                entities += get_pred_entities(obj[entity_type])
    return entities

def gen_metrics_entities(utterances, model_entities, test_gold, test_pred):
    # TODO: remove model_entities parameter?
    def filter_entities(model_entities, entities):
        return [e for e in entities if e["entity"] in model_entities]

    for gold_utt in test_gold["utterances"]:
        utterances[gold_utt["text"]]["gold_entities"] = filter_entities(model_entities, list(gold_utt["entities"]))
    for pred_utt in test_pred:
        utterances[pred_utt["query"]]["pred_entities"] = filter_entities(model_entities, get_pred_entities(pred_utt["prediction"]["entities"]))

def gen_metrics_intents(utterances, test_gold, test_pred):
    for gold_utt in test_gold["utterances"]:
        utterances[gold_utt["text"]]["gold_intent"] = gold_utt["intent"]
    for pred_utt in test_pred:
        if pred_utt["query"] in utterances:
            utterances[pred_utt["query"]]["pred_intent"] = pred_utt["prediction"]["topIntent"]
            utterances[pred_utt["query"]]["pred_intent_score"] = pred_utt["prediction"]["intents"][pred_utt["prediction"]["topIntent"]]["score"]

def init_utterances(test_gold):
    utterances = {}
    for utterance in test_gold["utterances"]:
        utterances[utterance["text"]] = {
            "query": utterance["text"],
            "gold_intent": "",
            "pred_intent": "",
            "gold_entities": [],
            "pred_entities": []}
    return utterances

def gen_utterance_metrics(model_entities, test_gold, test_pred):
    def convert_utterances(utterances):
        utt_list = []
        for key in utterances:
            utt_list += [utterances[key]]
        return utt_list

    utterances = init_utterances(test_gold)
    gen_metrics_intents(utterances, test_gold, test_pred)
    gen_metrics_entities(utterances, model_entities, test_gold, test_pred)
    return convert_utterances(utterances)

def get_model_entities(model):
    def get_entities(entity):
        entities = [entity["name"]]
        if "children" in entity:
            for child in entity["children"]:
                entities += get_entities(child)
        return entities
    entities = []
    for entity in model["entities"]:
        entities += get_entities(entity)
    return set(entities)

def get_model_intents(model):
    return [intent["name"] for intent in model["intents"]]

def gen_metrics(model_json_file, test_json_file, test_log_file):
    with open(model_json_file, "r") as f:
        model = json.load(f)

    with open(test_json_file, "r") as f:
        test_gold = json.load(f)

    with open(test_log_file, "r") as f:
        test_pred = json.load(f)

    model_entities = get_model_entities(model)
    return {
        "intents": get_model_intents(model),
        "entities": list(model_entities),
        "utterances": gen_utterance_metrics(model_entities, test_gold, test_pred)
        }

def save_metrics(output_folder, predictions):
    predictions_path = os.path.join(output_folder, "predictions.json")
    with open(predictions_path, "w") as f:
        json.dump(predictions, f, indent=1)
        print("%s written"%predictions_path)
    return predictions_path

def create_out_folder(config):
    luis_config = config["LuisConfig"]
    output_folder = os.path.join(config["OutputFolder"], config["Name"] + "_" + datetime.datetime.now().strftime('%Y-%m-%d_%H-%M-%S'))
    os.makedirs(output_folder)

    # Copy config files
    config_folder = os.path.join(output_folder, 'input')
    os.makedirs(config_folder)

    if "LuisConfig" in config and "TrainLu" in luis_config:
        shutil.copy(luis_config["TrainLu"], config_folder)
    if "LuisConfig" in config and "TestLu" in luis_config:
        shutil.copy(luis_config["TestLu"], config_folder)

    return output_folder

def read_params():
    try:
        opts, _ = getopt.getopt(argv[2:], '', ['name=', 'output=', 'botname=', 'region=', 'authoringkey=', 'predictionkey=', 'trainlu=', 'testlu=', 'dialog=','suffix=', 'authoringendpoint=', 'predictionendpoint='])
        
        name = ''
        output = ''
        bot_name = ''
        region = ''
        authoring_key = ''
        prediction_key = ''
        train_lu = ''
        test_lu = ''
        dialog = ''
        suffix = ''
        authoring_endpoint = ''
        prediction_endpoint = ''

        for opt, arg in opts:
            if opt in ('--name'):
                name = arg
            if opt in ('--output'):
                output = arg
            if opt in ('--botname'):
                bot_name = arg
            if opt in ('--region'):
                region = arg
            if opt in ('--authoringkey'):
                authoring_key = arg
            if opt in ('--predictionkey'):
                prediction_key = arg
            if opt in ('--trainlu'):
                train_lu = arg
            if opt in ('--testlu'):
                test_lu = arg
            if opt in ('--dialog'):
                dialog = arg
            if opt in ('--suffix'):
                suffix = arg
            if opt in ('--authoringendpoint'):
                authoring_endpoint = arg
            if opt in ('--predictionendpoint'):
                prediction_endpoint = arg
    except Exception as e:
        print(e.Message)
        print('Exception parsing parameters. Usage:')
        print('main.py (build|test|build_test) --config=file.json')
        exit(2)

    config = {'Name' : name, 'OutputFolder' : output, 'LuisConfig' : { 'BotName' : bot_name, 'AuthoringKey' : authoring_key, 'Region' : region, 'TrainLu' : train_lu, 'TestLu' : test_lu, 'Dialog' : dialog, 'Suffix' : suffix, 'AuthoringEndpoint' : authoring_endpoint, 'PredictionKey' : prediction_key, 'PredictionEndpoint' : prediction_endpoint }}
    
    return argv[1], config

def produce_metrics(predictions_path, metrics_output_dir, model_json_file):
    '''
    generate:
        <metrics_output_dir>\ClassificationErrorAnalysisReport.json
        <metrics_output_dir>\EntitiesErrorAnalysisReport.json
        <metrics_output_dir>\intent_stat.json
        <metrics_output_dir>\intent_stat.txt
        <metrics_output_dir>\entity_stat.json
        <metrics_output_dir>\entity_stat.txt
    '''
    from metrics.code.convert import convert
    from metrics.code.config import Options 
    from metrics.code.main import process_collect_stat

    if os.path.exists(metrics_output_dir) is False:
        os.makedirs(metrics_output_dir)

    # intermediate files
    convert(
        predictions_path,
        metrics_output_dir
    )

    intent_error_analysis_report = os.path.join(
        metrics_output_dir,
        "ClassificationErrorAnalysisReport.json"
    )

    entity_error_analysis_report = os.path.join(
        metrics_output_dir,
        "EntitiesErrorAnalysisReport.json"
    )

    if os.path.exists(intent_error_analysis_report) is False or \
       os.path.exists(entity_error_analysis_report) is False:
       raise Exception('necessary files %s and %s missing'%(
         intent_error_analysis_report,
         entity_error_analysis_report))
    
    with open(model_json_file, "r") as fd:
        model = json.load(fd)
    model_intents = sorted(get_model_intents(model))
    model_entities = sorted(get_model_entities(model))

    # metric stats
    res_dir_list = [metrics_output_dir]
    res_dir_description_list = ["metrics"]
    options = Options()
    options.dic["collect_stat"] = {
            "enable": True,
            "entity": {
                "Files": [
                    "EntitiesErrorAnalysisReport.json"
                ],
                "LabelSet": set(model_entities),
                "LabelKey": "Entity",
                "PrintLabelOrder": model_entities
            },
            "intent": {
                "Files": [
                    "ClassificationErrorAnalysisReport.json"
                ],
                "LabelSet": set(model_intents),
                "LabelKey": "Class",
                "PrintLabelOrder": model_intents
            }
        }

    process_collect_stat(
        res_dir_list,
        res_dir_description_list,
        metrics_output_dir,
        options
    )

    return

def main():
    mode, config  = read_params()
    luis_config = config["LuisConfig"]

    output_folder = create_out_folder(config)

    if mode == 'build':
        build(luis_config, output_folder)
    elif mode == 'test':
        test(output_folder, GetConfig(luis_config, "PredictionKey"), GetConfig(luis_config, "PredictionEndpoint"), luis_config["AppId"], luis_config["TestLu"], "Test")
    elif mode == 'build_test':
        appid, model_json_file = build(luis_config, output_folder)
        test_pred_file, test_gold_file = test(output_folder, GetConfig(luis_config, "PredictionKey"), GetConfig(luis_config, "PredictionEndpoint"), appid, luis_config["TestLu"], "Test")
        predictions = gen_metrics(model_json_file, test_gold_file, test_pred_file)
        predictions_path = save_metrics(output_folder, predictions)
        # metric stats
        produce_metrics(
            predictions_path,
            os.path.join(output_folder, "metrics"),
            test_gold_file)

if __name__ == '__main__':
    main()
