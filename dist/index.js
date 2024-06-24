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
var __importDefault = (this && this.__importDefault) || function (mod) {
    return (mod && mod.__esModule) ? mod : { "default": mod };
};
Object.defineProperty(exports, "__esModule", { value: true });
const client_1 = require("@prisma/client");
const express_1 = __importDefault(require("express"));
const port = 3001;
const prisma = new client_1.PrismaClient();
const app = (0, express_1.default)();
app.use(express_1.default.json());
app.use(function (req, res, next) {
    res.header("Access-Control-Allow-Origin", "*");
    res.header("Access-Control-Allow-Headers", "Origin, X-Requested-With, Content-Type, Accept");
    next();
});
app.post('/signUp', (req, res) => __awaiter(void 0, void 0, void 0, function* () {
    const { name, email, password } = req.body;
    const sameUser = yield prisma.user.findFirst({ where: { OR: [{ email }, { name }] } });
    if (sameUser) {
        res.status(303).send();
        return;
    }
    try {
        yield prisma.user.create({
            data: {
                name,
                email,
                password
            },
        });
    }
    catch (e) {
        console.error(e);
    }
    res.status(200).send();
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