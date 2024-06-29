import { PrismaClient } from '@prisma/client'
import express from 'express'
import { getLatestClientRelease } from './getLatestClientRelease'
import * as argon2 from 'argon2'
import jwt from 'jsonwebtoken'

// eslint-disable-next-line @typescript-eslint/ban-ts-comment
// @ts-expect-error
BigInt.prototype['toJSON'] = function () {
  return this.toString()
}

const port = 3001
const prisma = new PrismaClient()
const app = express()

app.use(express.json())

app.use(function (req, res, next) {
  res.header('Access-Control-Allow-Origin', '*')
  res.header('Access-Control-Allow-Headers', 'Origin, X-Requested-With, Content-Type, Accept')
  next()
})

app.post('/signUp', async (req, res) => {
  const { name, email, password } = req.body

  const sameUser = await prisma.user.findFirst({ where: { OR: [{ email }, { name }] } })

  if (sameUser) {
    res.status(303).send()
    return
  }

  const hashedPassword = await argon2.hash(password)

  try {
    await prisma.user.create({
      data: {
        name,
        email,
        password: hashedPassword,
      },
    })
  } catch (e) {
    console.error(e)
  }

  res.status(200).send()
})

app.post('/signIn', async (req, res) => {
  const { email, password } = req.body

  const user = await prisma.user.findFirst({ where: { email } })

  if (!user) {
    res.status(404).send()
    return
  }

  if (!(await argon2.verify(user.password, password))) {
    res.status(409).send()
    return
  }

  const token = jwt.sign({ id: user?.id, email: user?.email }, process.env.JWT_PRIVATE_KEY, {
    expiresIn: '1h',
  })

  res.status(200).json({ token }).send()
})

app.get('/client-version', async (req, res) => {
  const repositoryOwner = 'Immortal-Vault'
  const repositoryName = 'Client'

  const version = await getLatestClientRelease(repositoryOwner, repositoryName)
  return res.status(200).send({
    version: version.replace('v', ''),
    downloadUrl: `https://github.com/${repositoryOwner}/${repositoryName}/releases/download/${version}/Immortal.Vault.Setup.exe`,
  })
})

app.get('/', (req, res) => {
  return res.send('Immortal Vault Server')
})

app.get('/ping', async (req, res) => {
  res.status(200).send('Pong!')
})

app.listen(port, () => console.log(`Server started on port ${port}`))

module.exports = app
