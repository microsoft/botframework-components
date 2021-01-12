import fs from "fs";
import path from "path";
import restify from "restify";
import { AdaptiveBot } from "./adaptiveBot";
import { ConversationState, useBotState, UserState } from "botbuilder";
import { RuntimeConfig } from "botbuilder-runtime-core";
import { loadRuntime } from "botbuilder-runtime";

(async function () {
  const runtimeConfig: RuntimeConfig = JSON.parse(
    await new Promise<string>((resolve, reject) =>
      fs.readFile(
        path.join(__dirname, "..", "runtime.json"),
        "utf8",
        (err, data) => (err ? reject(err) : resolve(data))
      )
    )
  );

  const appSettings: Record<string, unknown> = JSON.parse(
    await new Promise<string>((resolve, reject) =>
      fs.readFile(
        path.join(__dirname, "..", "appsettings.json"),
        "utf8",
        (err, data) => (err ? reject(err) : resolve(data))
      )
    )
  );

  const { adapter, storage } = await loadRuntime(runtimeConfig, appSettings);

  const userState = new UserState(storage);
  const conversationState = new ConversationState(storage);
  useBotState(adapter, userState, conversationState);

  const bot = new AdaptiveBot(path.join(__dirname, '..', '..', 'composer'), userState, conversationState);

  const server = restify.createServer();

  server.post("/api/messages", async (req, res) => {
    await adapter.processActivity(req, res, async (turnContext) => {
      await bot.onTurnActivity(turnContext);
    });
  });

  const port = process.env.port ?? process.env.PORT ?? 3978;
  server.listen(port, () => {
    console.log(`server listening on ${port}...`);
  });

  await new Promise<void>((resolve, reject) => {
    server.on("close", resolve);
    server.on("error", reject);
  });
})()
  .then(() => process.exit())
  .catch(console.error)
  .then(() => process.exit(1));
