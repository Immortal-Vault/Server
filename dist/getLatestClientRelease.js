"use strict";
var __awaiter = (this && this.__awaiter) || function (thisArg, _arguments, P, generator) {
    function adopt(value) { return value instanceof P ? value : new P(function (resolve) { resolve(value); }); }
    return new (P || (P = Promise))(function (resolve, reject) {
        function fulfilled(value) { try { step(generator.next(value)); } catch (e) { reject(e); } }
        function rejected(value) { try { step(generator["throw"](value)); } catch (e) { reject(e); } }
        function step(result) { result.done ? resolve(result.value) : adopt(result.value).then(fulfilled, rejected); }
        step((generator = generator.apply(thisArg, _arguments || [])).next());
    });
};
Object.defineProperty(exports, "__esModule", { value: true });
exports.getLatestClientRelease = getLatestClientRelease;
function getLatestClientRelease(repositoryOwner, repositoryName) {
    return __awaiter(this, void 0, void 0, function* () {
        const url = `https://api.github.com/repos/${repositoryOwner}/${repositoryName}/releases/latest`;
        try {
            const response = yield fetch(url, {
                headers: {
                    Authorization: `token ${process.env.GITHUB_TOKEN}`,
                },
            });
            if (!response.ok) {
                console.error(`Network response was not ok: ${response.statusText}`);
            }
            const data = yield response.json();
            return data.tag_name;
        }
        catch (error) {
            console.error('Error fetching the latest release:', error);
        }
    });
}
//# sourceMappingURL=getLatestClientRelease.js.map