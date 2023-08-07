// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

/**
 * Data describing a CLU application.
 */
export class CluApplication {
  /**
   * Initializes a new instance of the CluApplication class.
   * @param projectName CLU project name.
   * @param endpointKey CLU subscription or endpoint key.
   * @param endpoint CLU endpoint to use.
   * @param deploymentName CLU deployment name.
   */
  constructor(
    public projectName: string,
    public endpointKey: string,
    public endpoint: string,
    public deploymentName: string
  ) {
    if (!projectName?.trim()) {
      throw new Error(`CLU "projectName" parameter cannot be null or empty.`);
    }

    if (!this.isWellFormatedGUID(endpointKey)) {
      throw new Error(`"${endpointKey}" is not a valid CLU subscription key.`);
    }

    if (!endpoint?.trim()) {
      throw new Error(`CLU "endpoint" parameter cannot be null or empty.`);
    }

    if (!this.isWellFormedUriString(endpoint)) {
      throw new Error(`"${endpoint}" is not a valid CLU endpoint.`);
    }

    if (!deploymentName?.trim()) {
      throw new Error(
        `CLU "deploymentName" parameter cannot be null or empty.`
      );
    }
  }

  /**
   * Check if the provided value is a well formated GUID.
   * @param guid The GUID value.
   * @returns True if the GUID is well formated.
   */
  private isWellFormatedGUID(guid: string) {
    const pattern = /^(((?=.*}$){)|((?!.*}$)))((?!.*-.*)|(?=(.*[-].*){4}))[0-9a-fA-F]{8}[-]?([0-9a-fA-F]{4}[-]?){3}[0-9a-fA-F]{12}?[}]?$/m;
    return !!guid.match(pattern);
  }

  /**
   * Check if the provided value is a well formated URI.
   * @param uri The URI string value.
   * @returns True if the URI is well formated.
   */
  private isWellFormedUriString(uri: string): boolean {
    try {
      const uriResult = new URL(uri);
      return (
        (uriResult.toString() === uri || uriResult.toString() === `${uri}/`) &&
        (uriResult.protocol === 'https:' || uriResult.protocol === 'http:')
      );
    } catch {
      return false;
    }
  }
}
