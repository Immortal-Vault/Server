"use strict";
var __createBinding = (this && this.__createBinding) || (Object.create ? (function(o, m, k, k2) {
    if (k2 === undefined) k2 = k;
    var desc = Object.getOwnPropertyDescriptor(m, k);
    if (!desc || ("get" in desc ? !m.__esModule : desc.writable || desc.configurable)) {
      desc = { enumerable: true, get: function() { return m[k]; } };
    }
    Object.defineProperty(o, k2, desc);
}) : (function(o, m, k, k2) {
    if (k2 === undefined) k2 = k;
    o[k2] = m[k];
}));
var __setModuleDefault = (this && this.__setModuleDefault) || (Object.create ? (function(o, v) {
    Object.defineProperty(o, "default", { enumerable: true, value: v });
}) : function(o, v) {
    o["default"] = v;
});
var __importStar = (this && this.__importStar) || function (mod) {
    if (mod && mod.__esModule) return mod;
    var result = {};
    if (mod != null) for (var k in mod) if (k !== "default" && Object.prototype.hasOwnProperty.call(mod, k)) __createBinding(result, mod, k);
    __setModuleDefault(result, mod);
    return result;
};
var __awaiter = (this && this.__awaiter) || function (thisArg, _arguments, P, generator) {
    function adopt(value) { return value instanceof P ? value : new P(function (resolve) { resolve(value); }); }
    return new (P || (P = Promise))(function (resolve, reject) {
        function fulfilled(value) { try { step(generator.next(value)); } catch (e) { reject(e); } }
        function rejected(value) { try { step(generator["throw"](value)); } catch (e) { reject(e); } }
        function step(result) { result.done ? resolve(result.value) : adopt(result.value).then(fulfilled, rejected); }
        step((generator = generator.apply(thisArg, _arguments || [])).next());
    });
};
var __importDefault = (this && this.__importDefault) || function (mod) {
    return (mod && mod.__esModule) ? mod : { "default": mod };
};
Object.defineProperty(exports, "__esModule", { value: true });
const client_1 = require("@prisma/client");
const express_1 = __importDefault(require("express"));
const getLatestClientRelease_1 = require("./getLatestClientRelease");
const argon2 = __importStar(require("argon2"));
const jsonwebtoken_1 = __importDefault(require("jsonwebtoken"));
// eslint-disable-next-line @typescript-eslint/ban-ts-comment
// @ts-expect-error
BigInt.prototype['toJSON'] = function () {
    return this.toString();
};
const port = 3001;
const prisma = new client_1.PrismaClient();
const app = (0, express_1.default)();
app.use(express_1.default.json());
app.use(function (req, res, next) {
    res.header('Access-Control-Allow-Origin', '*');
    res.header('Access-Control-Allow-Headers', 'Origin, X-Requested-With, Content-Type, Accept');
    next();
});
app.post('/signUp', (req, res) => __awaiter(void 0, void 0, void 0, function* () {
    const { name, email, password } = req.body;
    const sameUser = yield prisma.user.findFirst({ where: { OR: [{ email }, { name }] } });
    if (sameUser) {
        res.status(303).send();
        return;
    }
    const hashedPassword = yield argon2.hash(password);
    try {
        yield prisma.user.create({
            data: {
                name,
                email,
                password: hashedPassword,
            },
        });
    }
    catch (e) {
        console.error(e);
    }
    res.status(200).send();
}));
app.post('/signIn', (req, res) => __awaiter(void 0, void 0, void 0, function* () {
    const { email, password } = req.body;
    const user = yield prisma.user.findFirst({ where: { email } });
    if (!user) {
        res.status(404).send();
        return;
    }
    if (!(yield argon2.verify(user.password, password))) {
        res.status(409).send();
        return;
    }
    const token = jsonwebtoken_1.default.sign({ id: user === null || user === void 0 ? void 0 : user.id, email: user === null || user === void 0 ? void 0 : user.email }, process.env.JWT_PRIVATE_KEY, {
        expiresIn: '1h',
    });
    res.status(200).json({ token }).send();
}));
app.get('/client-version', (req, res) => __awaiter(void 0, void 0, void 0, function* () {
    const repositoryOwner = 'Immortal-Vault';
    const repositoryName = 'Client';
    const version = yield (0, getLatestClientRelease_1.getLatestClientRelease)(repositoryOwner, repositoryName);
    return res.status(200).send({
        version: version.replace('v', ''),
        downloadUrl: `https://github.com/${repositoryOwner}/${repositoryName}/releases/download/${version}/Immortal.Vault.Setup.exe`,
    });
}));
app.get('/', (req, res) => {
    return res.send('Immortal Vault Server');
});
app.get('/ping', (req, res) => __awaiter(void 0, void 0, void 0, function* () {
    res.status(200).send('Pong!');
}));
app.listen(port, () => console.log(`Server started on port ${port}`));
module.exports = app;
//# sourceMappingURL=index.js.map