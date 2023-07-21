// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

export class CluApplication {
  constructor(
    public projectName: string,
    public endpointKey: string,
    public endpoint: string,
    public deploymentName: string
  ) {
    if (!projectName?.trim()) {
      throw new Error(`CLU "projectName" parameter cannot be null or empty.`);
    }

    if (!this.isGUID(endpointKey)) {
      // TODO: Implement this => (!Guid.TryParse(endpointKey, out var _))
      throw new Error(`"${endpointKey}" is not a valid CLU subscription key.`);
    }

    if (!endpoint?.trim()) {
      throw new Error(`CLU "endpoint" parameter cannot be null or empty.`);
    }

    if (!this.isWellFormedUriString(endpoint)) {
      // TODO: Implement this => (!Uri.IsWellFormedUriString(endpoint, UriKind.Absolute))
      throw new Error(`"${endpoint}" is not a valid CLU endpoint.`);
    }

    if (!deploymentName?.trim()) {
      throw new Error(
        `CLU "deploymentName" parameter cannot be null or empty.`
      );
    }
  }

  private isGUID(guid: string){
    var pattern = /^(((?=.*}$){)|((?!.*}$)))((?!.*-.*)|(?=(.*[-].*){4}))[0-9a-fA-F]{8}[-]?([0-9a-fA-F]{4}[-]?){3}[0-9a-fA-F]{12}?[}]?$/m;
    return !!guid.match(pattern);
  }

  private isWellFormedUriString(uri: string): boolean {
    try {
      const uriResult = new URL(uri);
      return ((uriResult.toString() === uri || uriResult.toString() === `${uri}/`) &&
        (uriResult.protocol === "https:" || uriResult.protocol === "http:"));
    } catch (err) {
      return false;
    }
  };
}
